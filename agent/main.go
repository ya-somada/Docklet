// Package main は Docklet WSL Agent 本体である。
// Windows GUI からの要求を stdin/stdout 上の 1 行 1 JSON で受け取り、
// /var/run/docker.sock 経由で Docker Engine API を呼び出す。
package main

import (
	"bufio"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"os"

	"docklet/agent/internal/docker"
	"docklet/agent/methods"
	_ "docklet/agent/methods/containers"
	_ "docklet/agent/methods/images"
	_ "docklet/agent/methods/networks"
	_ "docklet/agent/methods/system"
	_ "docklet/agent/methods/volumes"
)

// agent は登録済みメソッドを保持する Agent 本体。
type agent struct {
	methods map[string]methods.Method
}

func main() {
	if err := run(os.Stdin, os.Stdout); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}

// run は in から 1 行 1 JSON の要求を読み込み、応答を out へ書き出すメインループ。
// 1 行の解析に失敗しても処理は継続し、入出力自体のエラーでのみ終了する。
func run(in io.Reader, out io.Writer) error {
	a := &agent{methods: methods.All(docker.New())}
	scanner := bufio.NewScanner(in)
	scanner.Buffer(make([]byte, 0, 64*1024), 16*1024*1024)
	encoder := json.NewEncoder(out)
	encoder.SetEscapeHTML(false)

	for scanner.Scan() {
		line := scanner.Bytes()
		if len(line) == 0 {
			continue
		}

		var req request
		if err := json.Unmarshal(line, &req); err != nil {
			if encodeErr := encoder.Encode(errorResponse("", "JSON を解析できません")); encodeErr != nil {
				return encodeErr
			}
			continue
		}

		resp := a.handle(context.Background(), req)
		if err := encoder.Encode(resp); err != nil {
			return err
		}
	}

	return scanner.Err()
}
