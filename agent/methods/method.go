// Package methods は Agent が扱う各メソッド (Docker 操作 1 つ分) の共通インターフェースと、
// メソッドをレジストリへ登録・列挙する仕組みを提供する。
// 実装は init() で Register を呼び、自身を登録する。
package methods

import (
	"context"
	"encoding/json"

	"docklet/agent/internal/docker"
)

// Method は Windows から呼ばれる 1 つの操作の規定。
type Method interface {
	// Name は JSON の method と一致する名前を返す。
	Name() string
	// Handle は params を受け、成功時は result を返す。
	Handle(ctx context.Context, params json.RawMessage) (any, error)
}

// Base は各メソッドが埋め込む規定構造体。
type Base struct {
	name string
}

// NewBase はメソッド名を持つ規定構造体を返す。
func NewBase(name string) Base {
	return Base{name: name}
}

// Name はメソッド名を返す。
func (b Base) Name() string {
	return b.name
}

// Factory は Docker クライアントからメソッドを生成する。
type Factory func(client *docker.Client) Method

// factories は Register で登録されたメソッド生成関数の一覧。
var factories []Factory

// Register はメソッドを Agent に登録する。
func Register(factory Factory) {
	factories = append(factories, factory)
}

// All は登録済みメソッドを名前で引ける地図にして返す。
func All(client *docker.Client) map[string]Method {
	table := make(map[string]Method, len(factories))
	for _, factory := range factories {
		method := factory(client)
		table[method.Name()] = method
	}
	return table
}
