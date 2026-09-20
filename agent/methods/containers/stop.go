package containers

import (
	"context"
	"encoding/json"
	"net/url"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const stopName = "containers.stop"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newStop(client)
	})
}

// Stop はコンテナーを停止するメソッド。
type Stop struct {
	methods.Base
	docker *docker.Client
}

func newStop(client *docker.Client) *Stop {
	return &Stop{
		Base:   methods.NewBase(stopName),
		docker: client,
	}
}

// Handle は POST /containers/{id}/stop を呼ぶ。
func (m *Stop) Handle(ctx context.Context, params json.RawMessage) (any, error) {
	id, err := parseContainerID(params)
	if err != nil {
		return nil, err
	}

	if err := m.docker.Post(ctx, "/containers/"+url.PathEscape(id)+"/stop"); err != nil {
		return nil, err
	}

	return map[string]any{}, nil
}
