#!/usr/bin/env python3
"""Build docs/preview/index.html from the scratchpad sources.

  tiberium-dusk-preview.html + js3d_view.js
    -> assemble3d.py -> tiberium-dusk-3d.html
    -> wrap in <!doctype><head> (+viewport meta, build tag)
    -> inject window.IMPORTED_MAP (data/maps/sirocco.json) so the single-file
       game can swap in the imported Sirocco Flats map with no external fetch
    -> docs/preview/index.html   (committing this triggers the Pages deploy)

Usage:  python3 tools/web3d/build_index.py <build-tag>
"""
import os, sys, json, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SP = "/tmp/claude-0/-home-user-RedAlert/84746fb6-337c-560d-9167-231af8584ff6/scratchpad"
TAG = sys.argv[1] if len(sys.argv) > 1 else "dev"

subprocess.run([sys.executable, os.path.join(ROOT, "tools", "web3d", "assemble3d.py")], check=True)

body = open(os.path.join(SP, "tiberium-dusk-3d.html")).read()

mp = os.path.join(ROOT, "data", "maps", "sirocco.json")
inject = ""
if os.path.exists(mp):
    data = open(mp).read()                     # already minified JSON
    inject = "<script>window.IMPORTED_MAP=%s;</script>\n" % data

head = ('<!doctype html>\n<html>\n<head><meta charset="utf-8">'
        '<meta name="viewport" content="width=device-width, initial-scale=1">'
        '<meta http-equiv="Cache-Control" content="no-cache, no-store, must-revalidate">'
        '<meta http-equiv="Pragma" content="no-cache">'
        '<!-- build: %s --></head>\n<body>\n%s' % (TAG, inject))

out = head + body
dst = os.path.join(ROOT, "docs", "preview", "index.html")
open(dst, "w").write(out)
print("WROTE docs/preview/index.html: %d KB  (map injected: %s)" % (len(out) // 1024, bool(inject)))
