package main

import (
	"context"
	"encoding/json"
	"fmt"
)

// request は Windows 側から送られる 1 行 JSON。
type request struct {
	ID     string          `json:"id"`
	Method string          `json:"method"`
	Params json.RawMessage `json:"params,omitempty"`
}

// response は Agent が stdout へ返す 1 行 JSON。
type response struct {
	ID     string `json:"id"`
	OK     bool   `json:"ok"`
	Result any    `json:"result,omitempty"`
	Error  string `json:"error,omitempty"`
}

// handle は 1 件の要求を method 名で対応するメソッドへディスパッチする。
// 未登録のメソッド名やメソッド自体のエラーは、エラー応答へ変換して返す。
func (a *agent) handle(ctx context.Context, req request) response {
	method, ok := a.methods[req.Method]
	if !ok {
		return errorResponse(req.ID, fmt.Sprintf("未知のメソッドです: %s", req.Method))
	}

	result, err := method.Handle(ctx, req.Params)
	if err != nil {
		return errorResponse(req.ID, err.Error())
	}

	return response{ID: req.ID, OK: true, Result: result}
}

// errorResponse は id に紐づくエラー応答を組み立てる。
func errorResponse(id, message string) response {
	return response{ID: id, OK: false, Error: message}
}
