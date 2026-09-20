package containers

import (
	"context"
	"encoding/binary"
	"encoding/json"
	"net/url"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const (
	logsName     = "containers.logs"
	logsTail     = "1000"
	headerLength = 8
)

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newLogs(client)
	})
}

// Logs はコンテナーログを取得するメソッド。
type Logs struct {
	methods.Base
	docker *docker.Client
}

func newLogs(client *docker.Client) *Logs {
	return &Logs{
		Base:   methods.NewBase(logsName),
		docker: client,
	}
}

// logsResult は Handle が返すログ本文。
type logsResult struct {
	Text string `json:"text"`
}

// Handle は GET /containers/{id}/logs の本文を返す。
func (m *Logs) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	id, err := parseContainerID(params)
	if err != nil {
		return nil, err
	}

	escaped := url.PathEscape(id)
	raw, err := m.docker.GetBytes(ctx, "/containers/"+escaped+"/logs?stdout=1&stderr=1&timestamps=1&tail="+logsTail)
	if err != nil {
		return nil, err
	}

	return logsResult{Text: strings.ToValidUTF8(decodeLogs(raw), "\uFFFD")}, nil
}

// decodeLogs は Docker のストリーム多重化フォーマット (stdout/stderr が
// 8 バイトヘッダー付きで交互に連結されたもの) を解いて、ログ本文だけを取り出す。
// 多重化されていない場合は raw をそのまま文字列化する。
func decodeLogs(raw []byte) string {
	if !isMultiplexed(raw) {
		return string(raw)
	}

	var builder strings.Builder
	offset := 0
	for offset+headerLength <= len(raw) {
		size := int(binary.BigEndian.Uint32(raw[offset+4 : offset+headerLength]))
		offset += headerLength
		if offset+size > len(raw) {
			return string(raw)
		}
		builder.Write(raw[offset : offset+size])
		offset += size
	}

	return builder.String()
}

// isMultiplexed は raw が Docker のストリーム多重化フォーマットのヘッダーで
// 始まっているかどうかを判定する。
func isMultiplexed(raw []byte) bool {
	if len(raw) < headerLength {
		return false
	}
	if raw[0] > 2 || raw[1] != 0 || raw[2] != 0 || raw[3] != 0 {
		return false
	}
	size := int(binary.BigEndian.Uint32(raw[4:headerLength]))
	return size >= 0 && headerLength+size <= len(raw)
}
