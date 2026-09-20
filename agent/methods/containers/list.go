package containers

import (
	"context"
	"encoding/json"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const listName = "containers.list"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newList(client)
	})
}

// List はコンテナー一覧を取得するメソッド。
type List struct {
	methods.Base
	docker *docker.Client
}

func newList(client *docker.Client) *List {
	return &List{
		Base:   methods.NewBase(listName),
		docker: client,
	}
}

// dockerContainer は GET /containers/json 応答の要素 1 件。
type dockerContainer struct {
	ID     string   `json:"Id"`
	Names  []string `json:"Names"`
	State  string   `json:"State"`
	Status string   `json:"Status"`
}

// listItem は Handle が返す一覧の要素 1 件。
type listItem struct {
	ID     string `json:"id"`
	Name   string `json:"name"`
	State  string `json:"state"`
	Status string `json:"status"`
}

// listResult は Handle が返すコンテナー一覧全体。
type listResult struct {
	Containers []listItem `json:"containers"`
}

// Handle は GET /containers/json?all=true の一覧を返す。
func (m *List) Handle(ctx context.Context, _ json.RawMessage) (any, error) {
	var raw []dockerContainer
	if err := m.docker.GetJSON(ctx, "/containers/json?all=true", &raw); err != nil {
		return nil, err
	}

	items := make([]listItem, 0, len(raw))
	for _, container := range raw {
		items = append(items, listItem{
			ID:     container.ID,
			Name:   containerName(container.Names, container.ID),
			State:  container.State,
			Status: container.Status,
		})
	}

	return listResult{Containers: items}, nil
}

// containerName は Docker の Names 配列から先頭の "/" を除いた表示名を返す。
// Names が空の場合は ID の先頭 12 文字 (短縮 ID) を返す。
func containerName(names []string, id string) string {
	if len(names) > 0 {
		return strings.TrimPrefix(names[0], "/")
	}
	if len(id) > 12 {
		return id[:12]
	}
	return id
}
