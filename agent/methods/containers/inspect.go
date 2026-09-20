package containers

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"
	"sort"
	"strconv"
	"strings"
	"time"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const inspectName = "containers.inspect"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newInspect(client)
	})
}

// Inspect はコンテナーの詳細を取得するメソッド。
type Inspect struct {
	methods.Base
	docker *docker.Client
}

func newInspect(client *docker.Client) *Inspect {
	return &Inspect{
		Base:   methods.NewBase(inspectName),
		docker: client,
	}
}

// dockerInspect は GET /containers/{id}/json 応答のうち、この Agent が使う部分だけを抜き出した構造体。
type dockerInspect struct {
	ID      string `json:"Id"`
	Created string `json:"Created"`
	Name    string `json:"Name"`
	State   struct {
		Status     string `json:"Status"`
		ExitCode   int    `json:"ExitCode"`
		StartedAt  string `json:"StartedAt"`
		FinishedAt string `json:"FinishedAt"`
		Health     *struct {
			Status string `json:"Status"`
		} `json:"Health"`
	} `json:"State"`
	Config struct {
		Env []string `json:"Env"`
	} `json:"Config"`
	NetworkSettings struct {
		Ports map[string][]dockerPortBinding `json:"Ports"`
	} `json:"NetworkSettings"`
	Mounts []dockerMount `json:"Mounts"`
}

// dockerPortBinding は NetworkSettings.Ports の 1 バインディング (ホスト側 IP・ポート)。
type dockerPortBinding struct {
	HostIp   string `json:"HostIp"`
	HostPort string `json:"HostPort"`
}

// dockerMount は Mounts の応答 1 件。
type dockerMount struct {
	Type        string `json:"Type"`
	Name        string `json:"Name"`
	Source      string `json:"Source"`
	Destination string `json:"Destination"`
	RW          bool   `json:"RW"`
}

// envItem は "KEY=VALUE" 形式の環境変数 1 件を name/value に分解したもの。
type envItem struct {
	Name  string `json:"name"`
	Value string `json:"value"`
}

// inspectResult は Handle が返すコンテナー詳細。
type inspectResult struct {
	ID         string      `json:"id"`
	Name       string      `json:"name"`
	State      string      `json:"state"`
	StatusText string      `json:"statusText"`
	Created    *string     `json:"created"`
	StartedAt  *string     `json:"startedAt"`
	Env        []envItem   `json:"env"`
	Ports      []portItem  `json:"ports"`
	Mounts     []mountItem `json:"mounts"`
}

// portItem はコンテナー側ポート 1 つに対する公開設定。
// ホストへ公開されていないポートは HostIP / HostPort が空文字列になる。
type portItem struct {
	ContainerPort int    `json:"containerPort"`
	Protocol      string `json:"protocol"`
	HostIP        string `json:"hostIp"`
	HostPort      string `json:"hostPort"`
}

// mountItem はコンテナーに割り当てられたマウント 1 件。
type mountItem struct {
	Type        string `json:"type"`
	Source      string `json:"source"`
	Destination string `json:"destination"`
	ReadOnly    bool   `json:"readOnly"`
}

// Handle は GET /containers/{id}/json の詳細を返す。
func (m *Inspect) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	id, err := parseContainerID(params)
	if err != nil {
		return nil, err
	}

	var raw dockerInspect
	if err := m.docker.GetJSON(ctx, "/containers/"+url.PathEscape(id)+"/json", &raw); err != nil {
		return nil, err
	}

	return inspectResult{
		ID:         raw.ID,
		Name:       containerName([]string{raw.Name}, raw.ID),
		State:      raw.State.Status,
		StatusText: formatStatusText(raw),
		Created:    dockerTime(raw.Created),
		StartedAt:  dockerTime(raw.State.StartedAt),
		Env:        parseEnv(raw.Config.Env),
		Ports:      parsePorts(raw.NetworkSettings.Ports),
		Mounts:     parseMounts(raw.Mounts),
	}, nil
}

// parsePorts は NetworkSettings.Ports ("8080/tcp" -> バインディング一覧) を
// コンテナー側ポート番号順に並んだ一覧へ変換する。
// バインディングが無いポート (公開されていないが EXPOSE されているポート) は
// HostIP / HostPort を空にした 1 件として残す。
func parsePorts(raw map[string][]dockerPortBinding) []portItem {
	items := make([]portItem, 0, len(raw))
	for key, bindings := range raw {
		port, protocol, ok := strings.Cut(key, "/")
		if !ok {
			continue
		}
		containerPort, err := strconv.Atoi(port)
		if err != nil {
			continue
		}

		if len(bindings) == 0 {
			items = append(items, portItem{ContainerPort: containerPort, Protocol: protocol})
			continue
		}

		for _, binding := range bindings {
			items = append(items, portItem{
				ContainerPort: containerPort,
				Protocol:      protocol,
				HostIP:        binding.HostIp,
				HostPort:      binding.HostPort,
			})
		}
	}

	sort.Slice(items, func(i, j int) bool {
		if items[i].ContainerPort != items[j].ContainerPort {
			return items[i].ContainerPort < items[j].ContainerPort
		}
		if items[i].Protocol != items[j].Protocol {
			return items[i].Protocol < items[j].Protocol
		}
		return items[i].HostPort < items[j].HostPort
	})

	return items
}

// parseMounts は Mounts の応答を表示用の一覧へ変換する。
// 名前付きボリュームは Source (実体のホストパス) の代わりにボリューム名を使う。
func parseMounts(raw []dockerMount) []mountItem {
	items := make([]mountItem, 0, len(raw))
	for _, mount := range raw {
		source := mount.Source
		if mount.Type == "volume" && mount.Name != "" {
			source = mount.Name
		}

		items = append(items, mountItem{
			Type:        mount.Type,
			Source:      source,
			Destination: mount.Destination,
			ReadOnly:    !mount.RW,
		})
	}

	return items
}

// parseEnv は "KEY=VALUE" 形式の一覧を envItem のスライスへ変換し、名前順に並べ替える。
// "=" を含まない不正なエントリは読み飛ばす。
func parseEnv(entries []string) []envItem {
	items := make([]envItem, 0, len(entries))
	for _, entry := range entries {
		name, value, _ := strings.Cut(entry, "=")
		if name == "" {
			continue
		}
		items = append(items, envItem{Name: name, Value: value})
	}
	sort.Slice(items, func(i, j int) bool {
		return items[i].Name < items[j].Name
	})
	return items
}

// formatStatusText は Docker CLI の `docker ps` に近いステータス文言を組み立てる。
func formatStatusText(raw dockerInspect) string {
	switch strings.ToLower(raw.State.Status) {
	case "running":
		text := "Up " + sinceText(raw.State.StartedAt)
		if raw.State.Health != nil && raw.State.Health.Status != "" {
			return text + " (" + raw.State.Health.Status + ")"
		}
		return text
	case "paused":
		return "Up " + sinceText(raw.State.StartedAt) + " (Paused)"
	case "restarting":
		return "Restarting"
	case "removing":
		return "Removing"
	case "dead":
		return "Dead"
	case "created":
		return "Created"
	case "exited":
		return fmt.Sprintf("Exited (%d) %s ago", raw.State.ExitCode, sinceText(raw.State.FinishedAt))
	default:
		return raw.State.Status
	}
}

// sinceText は Docker の時刻文字列 value から現在までの経過時間を、
// "3 minutes" のような英語表記 (docker ps 相当) で返す。解析できない場合は空文字列を返す。
func sinceText(value string) string {
	parsed := parseDockerTime(value)
	if parsed == nil {
		return ""
	}

	elapsed := time.Since(*parsed)
	if elapsed < 0 {
		elapsed = 0
	}

	switch {
	case elapsed < time.Second:
		return "Less than a second"
	case elapsed < time.Minute:
		n := int(elapsed.Seconds())
		if n == 1 {
			return "1 second"
		}
		return fmt.Sprintf("%d seconds", n)
	case elapsed < time.Hour:
		n := int(elapsed.Minutes())
		if n == 1 {
			return "1 minute"
		}
		return fmt.Sprintf("%d minutes", n)
	case elapsed < 48*time.Hour:
		n := int(elapsed.Hours())
		if n == 1 {
			return "1 hour"
		}
		return fmt.Sprintf("%d hours", n)
	default:
		n := int(elapsed.Hours() / 24)
		if n == 1 {
			return "1 day"
		}
		return fmt.Sprintf("%d days", n)
	}
}

// parseDockerTime は Docker が返す RFC3339 (Nano 含む) 形式の時刻文字列を解析する。
// 空文字列・ゼロ値・解析失敗の場合は nil を返す。
func parseDockerTime(value string) *time.Time {
	value = strings.TrimSpace(value)
	if value == "" {
		return nil
	}

	parsed, err := time.Parse(time.RFC3339Nano, value)
	if err != nil {
		parsed, err = time.Parse(time.RFC3339, value)
	}
	if err != nil || parsed.Year() <= 1 {
		return nil
	}

	return &parsed
}

// dockerTime は Docker の時刻文字列を UTC の RFC3339 表記へ正規化する。
// 解析できない場合は nil を返す。
func dockerTime(value string) *string {
	parsed := parseDockerTime(value)
	if parsed == nil {
		return nil
	}

	formatted := parsed.UTC().Format(time.RFC3339)
	return &formatted
}
