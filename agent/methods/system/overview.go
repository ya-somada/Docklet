package system

import (
	"context"
	"encoding/json"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const overviewName = "system.overview"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newOverview(client)
	})
}

// Overview は Docker エンジンの概要を取得するメソッド。
type Overview struct {
	methods.Base
	docker *docker.Client
}

func newOverview(client *docker.Client) *Overview {
	return &Overview{
		Base:   methods.NewBase(overviewName),
		docker: client,
	}
}

// dockerContainerState は GET /containers/json 応答の要素 1 件のうち、集計に必要な部分。
type dockerContainerState struct {
	State string `json:"State"`
}

// dockerNetworkSummary は GET /networks 応答の要素 1 件のうち、集計に必要な部分。
type dockerNetworkSummary struct {
	Name string `json:"Name"`
}

// dockerInfo は GET /info 応答のうち、表示に必要な部分。
type dockerInfo struct {
	ServerVersion   string `json:"ServerVersion"`
	OperatingSystem string `json:"OperatingSystem"`
}

// result は Handle が返す概要。
type result struct {
	RunningContainers int    `json:"runningContainers"`
	TotalContainers   int    `json:"totalContainers"`
	Images            int    `json:"images"`
	Volumes           int    `json:"volumes"`
	Networks          int    `json:"networks"`
	ServerVersion     string `json:"serverVersion"`
	OperatingSystem   string `json:"operatingSystem"`
}

// Handle は GET /containers/json, /images/json, /volumes, /networks, /info を集約して返す。
// /system/df はボリュームサイズ計算のため重くなり得るので使わない。
func (m *Overview) Handle(ctx context.Context, _ json.RawMessage) (any, error) {
	var containers []dockerContainerState
	if err := m.docker.GetJSON(ctx, "/containers/json?all=true", &containers); err != nil {
		return nil, err
	}

	var images []json.RawMessage
	if err := m.docker.GetJSON(ctx, "/images/json", &images); err != nil {
		return nil, err
	}

	var volumesResponse struct {
		Volumes []json.RawMessage `json:"Volumes"`
	}
	if err := m.docker.GetJSON(ctx, "/volumes", &volumesResponse); err != nil {
		return nil, err
	}

	var networks []dockerNetworkSummary
	if err := m.docker.GetJSON(ctx, "/networks", &networks); err != nil {
		return nil, err
	}

	var info dockerInfo
	if err := m.docker.GetJSON(ctx, "/info", &info); err != nil {
		return nil, err
	}

	res := result{
		TotalContainers: len(containers),
		Images:          len(images),
		Volumes:         len(volumesResponse.Volumes),
		ServerVersion:   info.ServerVersion,
		OperatingSystem: info.OperatingSystem,
	}

	for _, container := range containers {
		if container.State == "running" {
			res.RunningContainers++
		}
	}

	for _, network := range networks {
		if network.Name != "none" {
			res.Networks++
		}
	}

	return res, nil
}
