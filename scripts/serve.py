"""Serve published static files with a repository subpath for browser verification."""
import argparse
import functools
import http.server
import pathlib
import urllib.parse

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--port', type=int, required=True)
parser.add_argument('--base', default='/tennis/')
args = parser.parse_args()

class Handler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        path = urllib.parse.urlsplit(self.path).path
        if not path.startswith(args.base):
            self.send_error(404)
            return
        self.path = '/' + path[len(args.base):]
        candidate = pathlib.Path(args.root) / self.path.lstrip('/')
        if not candidate.is_file() and '.' not in self.path:
            self.path = '/index.html'
        super().do_GET()

    def end_headers(self):
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

    def log_message(self, *values):
        pass

http.server.ThreadingHTTPServer(('127.0.0.1', args.port), functools.partial(Handler, directory=args.root)).serve_forever()
