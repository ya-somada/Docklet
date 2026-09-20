package volumes

import (
	"context"
	"encoding/json"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const listName = "volumes.list"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newList(client)
	})
}

// List はボリューム一覧を取得するメソッド。
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

// dockerVolume は GET /volumes 応答の要素 1 件。
type dockerVolume struct {
	Name       string `json:"Name"`
	Driver     string `json:"Driver"`
	Mountpoint string `json:"Mountpoint"`
	CreatedAt  string `json:"CreatedAt"`
}

// volumesResponse は GET /volumes の応答全体。
type volumesResponse struct {
	Volumes []dockerVolume `json:"Volumes"`
}

// listItem は Handle が返す一覧の要素 1 件。
type listItem struct {
	Name       string `json:"name"`
	Driver     string `json:"driver"`
	Mountpoint string `json:"mountpoint"`
	CreatedAt  string `json:"createdAt"`
}

// listResult は Handle が返すボリューム一覧全体。
type listResult struct {
	Volumes []listItem `json:"volumes"`
}

// Handle は GET /volumes の一覧を返す。
func (m *List) Handle(ctx context.Context, _ json.RawMessage) (any, error) {
	var raw volumesResponse
	if err := m.docker.GetJSON(ctx, "/volumes", &raw); err != nil {
		return nil, err
	}

	items := make([]listItem, 0, len(raw.Volumes))
	for _, volume := range raw.Volumes {
		items = append(items, listItem{
			Name:       volume.Name,
			Driver:     volume.Driver,
			Mountpoint: volume.Mountpoint,
			CreatedAt:  volume.CreatedAt,
		})
	}

	return listResult{Volumes: items}, nil
}
