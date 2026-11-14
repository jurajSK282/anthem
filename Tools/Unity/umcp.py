"""Tiny client for the local Unity MCP HTTP server. Usage:
  umcp.py tools                 -> list tool names
  umcp.py schema <tool>         -> print input schema
  umcp.py call <tool> '<json>'  -> call tool
"""
import json, sys, urllib.request

URL = "http://localhost:8080/mcp"
H = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
sid = None

def rpc(method, params=None, notify=False):
    global sid
    body = {"jsonrpc": "2.0", "method": method}
    if params is not None: body["params"] = params
    if not notify: body["id"] = 1
    h = dict(H)
    if sid: h["mcp-session-id"] = sid
    req = urllib.request.Request(URL, json.dumps(body).encode(), h)
    with urllib.request.urlopen(req, timeout=180) as r:
        sid = r.headers.get("mcp-session-id") or sid
        raw = r.read().decode("utf-8", "replace")
    if notify: return None
    for line in raw.splitlines():
        if line.startswith("data:"):
            return json.loads(line[5:])
    return json.loads(raw) if raw.strip() else None

rpc("initialize", {"protocolVersion": "2025-03-26", "capabilities": {},
                   "clientInfo": {"name": "cli", "version": "1"}})
rpc("notifications/initialized", {}, notify=True)
cmd = sys.argv[1]
if cmd == "tools":
    for t in rpc("tools/list")["result"]["tools"]: print(t["name"])
elif cmd == "schema":
    for t in rpc("tools/list")["result"]["tools"]:
        if t["name"] == sys.argv[2]: print(json.dumps(t["inputSchema"].get("properties"), indent=1)[:6000])
elif cmd == "call":
    args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
    res = rpc("tools/call", {"name": sys.argv[2], "arguments": args})
    out = res.get("result", res)
    for c in out.get("content", []):
        if c.get("type") == "text": print(c["text"][:int(sys.argv[4]) if len(sys.argv) > 4 else 4000])
        else: print("[", c.get("type"), "]")
