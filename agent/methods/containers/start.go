package containers

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const startName = "containers.start"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newStart(client)
	})
}

// Start はコンテナーを起動するメソッド。
type Start struct {
	methods.Base
	docker *docker.Client
}

func newStart(client *docker.Client) *Start {
	return &Start{
		Base:   methods.NewBase(startName),
		docker: client,
	}
}

// Handle は POST /containers/{id}/start を呼ぶ。
func (m *Start) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	id, err := parseContainerID(params)
	if err != nil {
		return nil, err
	}

	if err := m.docker.Post(ctx, "/containers/"+url.PathEscape(id)+"/start"); err != nil {
		return nil, err
	}

	return map[string]any{}, nil
}

// parseContainerID は params の id フィールドからコンテナー ID を取り出す。
// フィールドが空、または存在しない場合はエラーを返す。
func parseContainerID(params json.RawMessage) (string, error) {
	var body struct {
		ID string `json:"id"`
	}
	if err := json.Unmarshal(params, &body); err != nil {
		return "", fmt.Errorf("コンテナー ID が指定されていません")
	}

	id := strings.TrimSpace(body.ID)
	if id == "" {
		return "", fmt.Errorf("コンテナー ID が指定されていません")
	}

	return id, nil
}
