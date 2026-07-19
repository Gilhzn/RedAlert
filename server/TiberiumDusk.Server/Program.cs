using System;
using TiberiumDusk.Server;

int port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 7777;
int players = args.Length > 1 && int.TryParse(args[1], out var n) ? n : 2;

using var server = new RelayServer(port, players);
server.Start();
Console.WriteLine($"Tiberium Dusk relay listening on ws://localhost:{port}/  (starts at {players} players)");
Console.WriteLine("Press Ctrl+C to stop.");

var quit = new System.Threading.ManualResetEvent(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };
quit.WaitOne();
