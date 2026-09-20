package networks

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const removeName = "networks.remove"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newRemove(client)
	})
}

// Remove はネットワークを削除するメソッド。
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

// Handle は DELETE /networks/{name} を呼ぶ。
func (m *Remove) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	name, err := parseNetworkName(params)
	if err != nil {
		return nil, err
	}

	if err := m.docker.Delete(ctx, "/networks/"+url.PathEscape(name)); err != nil {
		return nil, err
	}

	return map[string]any{}, nil
}

// parseNetworkName は params の name フィールドからネットワーク名を取り出す。
// フィールドが空、または存在しない場合はエラーを返す。
func parseNetworkName(params json.RawMessage) (string, error) {
	var body struct {
		Name string `json:"name"`
	}
	if err := json.Unmarshal(params, &body); err != nil {
		return "", fmt.Errorf("ネットワーク名が指定されていません")
	}

	name := strings.TrimSpace(body.Name)
	if name == "" {
		return "", fmt.Errorf("ネットワーク名が指定されていません")
	}

	return name, nil
}
