package images

import (
	"context"
	"encoding/json"
	"fmt"
	"net/url"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const removeName = "images.remove"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newRemove(client)
	})
}

// Remove はイメージを削除するメソッド。
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

// Handle は DELETE /images/{id} を呼ぶ。
func (m *Remove) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	id, err := parseImageId(params)
	if err != nil {
		return nil, err
	}

	if err := m.docker.Delete(ctx, "/images/"+url.PathEscape(id)); err != nil {
		return nil, err
	}

	return map[string]any{}, nil
}

// parseImageId は params の id フィールドからイメージ ID を取り出す。
// フィールドが空、または存在しない場合はエラーを返す。
func parseImageId(params json.RawMessage) (string, error) {
	var body struct {
		Id string `json:"id"`
	}
	if err := json.Unmarshal(params, &body); err != nil {
		return "", fmt.Errorf("イメージ ID が指定されていません")
	}

	id := strings.TrimSpace(body.Id)
	if id == "" {
		return "", fmt.Errorf("イメージ ID が指定されていません")
	}

	return id, nil
}
