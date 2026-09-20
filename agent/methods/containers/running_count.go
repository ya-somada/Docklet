package containers

import (
	"context"
	"encoding/json"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const runningCountName = "containers.runningCount"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newRunningCount(client)
	})
}

// RunningCount は起動中コンテナー数を取得するメソッド。
type RunningCount struct {
	methods.Base
	docker *docker.Client
}

func newRunningCount(client *docker.Client) *RunningCount {
	return &RunningCount{
		Base:   methods.NewBase(runningCountName),
		docker: client,
	}
}

// runningCountResult は Handle が返す起動中コンテナー数。
type runningCountResult struct {
	Count int `json:"count"`
}

// Handle は GET /containers/json の件数を返す。
func (m *RunningCount) Handle(ctx context.Context, _ json.RawMessage) (any, error) {
	var containers []json.RawMessage
	if err := m.docker.GetJSON(ctx, "/containers/json", &containers); err != nil {
		return nil, err
	}

	return runningCountResult{Count: len(containers)}, nil
}
