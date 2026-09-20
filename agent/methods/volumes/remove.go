package volumes

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const removeName = "volumes.remove"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newRemove(client)
	})
}

// Remove はボリュームを削除するメソッド。
type Remove struct {
	methods.Base
	docker *docker.Client
}

func newRemove(client *docker.Client) *Remove {
	return &Remove{
		Base:   methods.NewBase(removeName),
		docker: client,
	}
}

// Handle は DELETE /volumes/{name} を呼ぶ。
func (m *Remove) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	name, err := parseVolumeName(params)
	if err != nil {
		return nil, err
	}

	if err := m.docker.Delete(ctx, "/volumes/"+url.PathEscape(name)); err != nil {
		return nil, err
	}

	return map[string]any{}, nil
}

// parseVolumeName は params の name フィールドからボリューム名を取り出す。
// フィールドが空、または存在しない場合はエラーを返す。
func parseVolumeName(params json.RawMessage) (string, error) {
	var body struct {
		Name string `json:"name"`
	}
	if err := json.Unmarshal(params, &body); err != nil {
		return "", fmt.Errorf("ボリューム名が指定されていません")
	}

	name := strings.TrimSpace(body.Name)
	if name == "" {
		return "", fmt.Errorf("ボリューム名が指定されていません")
	}

	return name, nil
}
