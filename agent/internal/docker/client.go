// Package docker は Unix ソケット (/var/run/docker.sock) を介して
// Docker Engine API を呼び出すための薄い HTTP クライアントを提供する。
package docker

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net"
	"net/http"
	"strings"
	"time"
)

// sockPath は Docker デーモンが待ち受ける Unix ソケットのパス。
const sockPath = "/var/run/docker.sock"

// コンテナーのログなどによる無制限のメモリ消費を防ぐ。
const maxResponseBytes = 32 * 1024 * 1024

func readResponseBody(body io.Reader) ([]byte, error) {
	data, err := io.ReadAll(io.LimitReader(body, maxResponseBytes+1))
	if err != nil {
		return nil, fmt.Errorf("Docker Engine API の応答を読み取れません: %w", err)
	}
	if len(data) > maxResponseBytes {
		return nil, fmt.Errorf("Docker Engine API の応答が上限 (32 MiB) を超えました")
	}
	return data, nil
}

// Client は Unix ソケット経由で Docker Engine API を呼び出す。
type Client struct {
	http *http.Client
}

// New は /var/run/docker.sock に接続するクライアントを返す。
func New() *Client {
	transport := &http.Transport{
		DialContext: func(ctx context.Context, _, _ string) (net.Conn, error) {
			var dialer net.Dialer
			return dialer.DialContext(ctx, "unix", sockPath)
		},
	}

	return &Client{
		http: &http.Client{
			Timeout:   60 * time.Second,
			Transport: transport,
		},
	}
}

// GetJSON は Docker Engine API へ GET し、JSON を dest へデコードする。
func (c *Client) GetJSON(ctx context.Context, apiPath string, dest any) error {
	if !strings.HasPrefix(apiPath, "/") {
		apiPath = "/" + apiPath
	}

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, "http://localhost"+apiPath, nil)
	if err != nil {
		return err
	}

	resp, err := c.http.Do(req)
	if err != nil {
		return fmt.Errorf("docker ソケット (%s) に接続できません: %w", sockPath, err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("Docker Engine API が %d を返しました", resp.StatusCode)
	}

	body, err := readResponseBody(resp.Body)
	if err != nil {
		return err
	}
	if err := json.Unmarshal(body, dest); err != nil {
		return fmt.Errorf("Docker Engine API の応答を解析できません: %w", err)
	}

	return nil
}

// GetBytes は Docker Engine API へ GET し、本文を返す。
func (c *Client) GetBytes(ctx context.Context, apiPath string) ([]byte, error) {
	if !strings.HasPrefix(apiPath, "/") {
		apiPath = "/" + apiPath
	}

	req, err := http.NewRequestWithContext(ctx, http.MethodGet, "http://localhost"+apiPath, nil)
	if err != nil {
		return nil, err
	}

	resp, err := c.http.Do(req)
	if err != nil {
		return nil, fmt.Errorf("docker ソケット (%s) に接続できません: %w", sockPath, err)
	}
	defer resp.Body.Close()

	body, err := readResponseBody(resp.Body)
	if err != nil {
		return nil, err
	}

	if resp.StatusCode != http.StatusOK {
		var payload struct {
			Message string `json:"message"`
		}
		if json.Unmarshal(body, &payload) == nil && payload.Message != "" {
			return nil, fmt.Errorf("%s", payload.Message)
		}
		return nil, fmt.Errorf("Docker Engine API が %d を返しました", resp.StatusCode)
	}

	return body, nil
}

// Post は Docker Engine API へ POST する。本文は送らない。
func (c *Client) Post(ctx context.Context, apiPath string) error {
	return c.doNoBody(ctx, http.MethodPost, apiPath)
}

// Delete は Docker Engine API へ DELETE する。
func (c *Client) Delete(ctx context.Context, apiPath string) error {
	return c.doNoBody(ctx, http.MethodDelete, apiPath)
}

// doNoBody は本文を送らないリクエストを実行し、成功時は nil を返す。
func (c *Client) doNoBody(ctx context.Context, method, apiPath string) error {
	if !strings.HasPrefix(apiPath, "/") {
		apiPath = "/" + apiPath
	}

	req, err := http.NewRequestWithContext(ctx, method, "http://localhost"+apiPath, nil)
	if err != nil {
		return err
	}

	resp, err := c.http.Do(req)
	if err != nil {
		return fmt.Errorf("docker ソケット (%s) に接続できません: %w", sockPath, err)
	}
	defer resp.Body.Close()

	switch resp.StatusCode {
	case http.StatusOK, http.StatusNoContent, http.StatusNotModified:
		return nil
	}

	var payload struct {
		Message string `json:"message"`
	}
	body, err := readResponseBody(resp.Body)
	if err != nil {
		return err
	}
	if err := json.Unmarshal(body, &payload); err == nil && payload.Message != "" {
		return fmt.Errorf("%s", payload.Message)
	}

	return fmt.Errorf("Docker Engine API が %d を返しました", resp.StatusCode)
}
