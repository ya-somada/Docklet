package images

import (
	"context"
	"encoding/json"
	"strings"
	"time"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
)

const listName = "images.list"

func init() {
	methods.Register(func(client *docker.Client) methods.Method {
		return newList(client)
	})
}

// List はイメージ一覧を取得するメソッド。
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

// dockerImage は GET /images/json 応答の要素 1 件。
type dockerImage struct {
	Id       string   `json:"Id"`
	RepoTags []string `json:"RepoTags"`
	Created  int64    `json:"Created"`
	Size     int64    `json:"Size"`
}

// dockerContainer は GET /containers/json 応答の要素 1 件のうち、集計に必要な部分。
type dockerContainer struct {
	ImageID string `json:"ImageID"`
}

// listItem は Handle が返す一覧の要素 1 件。
type listItem struct {
	Id             string `json:"id"`
	Tags           string `json:"tags"`
	CreatedAt      string `json:"createdAt"`
	Size           int64  `json:"size"`
	ContainerCount int    `json:"containerCount"`
}

// listResult は Handle が返すイメージ一覧全体。
type listResult struct {
	Images []listItem `json:"images"`
}

// Handle は GET /images/json の一覧を返す。停止中も含めた各コンテナーの ImageID を突き合わせ、
// イメージごとの使用中コンテナー数を併せて返す。
func (m *List) Handle(ctx context.Context, _ json.RawMessage) (any, error) {
	var raw []dockerImage
	if err := m.docker.GetJSON(ctx, "/images/json", &raw); err != nil {
		return nil, err
	}

	var containers []dockerContainer
	if err := m.docker.GetJSON(ctx, "/containers/json?all=true", &containers); err != nil {
		return nil, err
	}

	containerCounts := make(map[string]int, len(containers))
	for _, container := range containers {
		containerCounts[container.ImageID]++
	}

	items := make([]listItem, 0, len(raw))
	for _, image := range raw {
		tags := make([]string, 0, len(image.RepoTags))
		for _, tag := range image.RepoTags {
			if tag != "" && tag != "<none>:<none>" {
				tags = append(tags, tag)
			}
		}

		items = append(items, listItem{
			Id:             image.Id,
			Tags:           strings.Join(tags, ", "),
			CreatedAt:      time.Unix(image.Created, 0).UTC().Format(time.RFC3339Nano),
			Size:           image.Size,
			ContainerCount: containerCounts[image.Id],
		})
	}

	return listResult{Images: items}, nil
}
