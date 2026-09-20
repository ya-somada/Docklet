package networks

import (
	"context"
	"encoding/json"
	"strings"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const listName = "networks.list"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newList(client)
	})
}

// List はネットワーク一覧を取得するメソッド。
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

// dockerNetwork は GET /networks 応答の要素 1 件。
type dockerNetwork struct {
	Id      string     `json:"Id"`
	Name    string     `json:"Name"`
	Driver  string     `json:"Driver"`
	Scope   string     `json:"Scope"`
	Created string     `json:"Created"`
	IPAM    dockerIPAM `json:"IPAM"`
}

// dockerIPAM は GET /networks 応答の IPAM 設定。
type dockerIPAM struct {
	Config []dockerIPAMConfig `json:"Config"`
}

// dockerIPAMConfig は IPAM 設定 1 件 (サブネット・ゲートウェイ)。
type dockerIPAMConfig struct {
	Subnet  string `json:"Subnet"`
	Gateway string `json:"Gateway"`
}

// dockerContainerNetworks は GET /containers/json 応答の要素 1 件のうち、集計に必要な部分。
type dockerContainerNetworks struct {
	NetworkSettings struct {
		Networks map[string]json.RawMessage `json:"Networks"`
	} `json:"NetworkSettings"`
}

// listItem は Handle が返す一覧の要素 1 件。
type listItem struct {
	Id             string `json:"id"`
	Name           string `json:"name"`
	Driver         string `json:"driver"`
	Scope          string `json:"scope"`
	CreatedAt      string `json:"createdAt"`
	Subnet         string `json:"subnet"`
	Gateway        string `json:"gateway"`
	ContainerCount int    `json:"containerCount"`
}

// listResult は Handle が返すネットワーク一覧全体。
type listResult struct {
	Networks []listItem `json:"networks"`
}

// Handle は GET /networks の一覧を返す。停止中も含めた各コンテナーの接続先ネットワーク名を突き合わせ、
// ネットワークごとの使用中コンテナー数を併せて返す。
func (m *List) Handle(ctx context.Context, _ json.RawMessage) (any, error) {
	var raw []dockerNetwork
	if err := m.docker.GetJSON(ctx, "/networks", &raw); err != nil {
		return nil, err
	}

	var containers []dockerContainerNetworks
	if err := m.docker.GetJSON(ctx, "/containers/json?all=true", &containers); err != nil {
		return nil, err
	}

	containerCounts := make(map[string]int)
	for _, container := range containers {
		for name := range container.NetworkSettings.Networks {
			containerCounts[name]++
		}
	}

	items := make([]listItem, 0, len(raw))
	for _, network := range raw {
		subnets := make([]string, 0, len(network.IPAM.Config))
		gateways := make([]string, 0, len(network.IPAM.Config))
		for _, config := range network.IPAM.Config {
			if config.Subnet != "" {
				subnets = append(subnets, config.Subnet)
			}
			if config.Gateway != "" {
				gateways = append(gateways, config.Gateway)
			}
		}

		items = append(items, listItem{
			Id:             network.Id,
			Name:           network.Name,
			Driver:         network.Driver,
			Scope:          network.Scope,
			CreatedAt:      network.Created,
			Subnet:         strings.Join(subnets, ", "),
			Gateway:        strings.Join(gateways, ", "),
			ContainerCount: containerCounts[network.Name],
		})
	}

	return listResult{Networks: items}, nil
}
