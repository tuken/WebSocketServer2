using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace WebSocketServer2
{
    public class Server
    {
        HttpListener _listener = new HttpListener();

        Thread _thread = null;

        /// <summary>
        /// クライアントのWebSocketインスタンスを格納
        /// </summary>
        HashSet<WebSocket> _clients = new HashSet<WebSocket>();

        /// <summary>
        /// 部屋を格納
        /// </summary>
        Dictionary<string, HashSet<WebSocket>> _rooms = new Dictionary<string, HashSet<WebSocket>>();

        public Server()
        {
            _listener.Prefixes.Add("http://+:2013/");
        }

        public void Start()
        {
            ThreadStart action = async () =>
            {
                _listener.Start();

                while (true)
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            };

            _thread = new Thread(action);
            _thread.Start();
        }

        public void Stop()
        {
            _listener.Stop();

            if (_thread != null)
            {
                _thread.Join();
                _thread = null;
            }
        }

        /// <summary>
        /// WebSocket接続毎の処理
        /// </summary>
        /// <param name="context"></param>
        async void ProcessRequest(HttpListenerContext context)
        {
            Console.WriteLine("{0}:New Session:{1}", DateTime.Now.ToString(), context.Request.RemoteEndPoint.Address.ToString());

            /// WebSocketの接続完了を待機してWebSocketオブジェクトを取得する
            var ws = (await (context.AcceptWebSocketAsync(null))).WebSocket;

            /// 新規クライアントを追加
            _clients.Add(ws);

            /// WebSocketの送受信ループ
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var buff = new ArraySegment<byte>(new byte[1024]);

                    /// 受信待機
                    var ret = await ws.ReceiveAsync(buff, CancellationToken.None);

                    /// テキスト
                    if (ret.MessageType == WebSocketMessageType.Text)
                    {
                        Console.WriteLine("{0}:String Received:{1}", DateTime.Now.ToString(), context.Request.RemoteEndPoint.Address.ToString());
                        string msgjson = Encoding.UTF8.GetString(buff.Take(ret.Count).ToArray());
                        Console.WriteLine("Message={0}", msgjson);

                        Message msg = MessageParser.Parse(msgjson);
                        if (msg.GetType() == typeof(JoinRoom))
                        {
                            HashSet<WebSocket> room = _rooms[ws.GetHashCode().ToString()];
                            room.Add(ws);
                            _rooms[ws.GetHashCode().ToString()] = room;
                            foreach (KeyValuePair<string, HashSet<WebSocket>> val in _rooms)
                            {
                                if (val.Key == ws.GetHashCode().ToString()) continue;
                                else
                                {
                                    val.Value.Contains(ws);
                                }
                            }
                        }
                        /// 各クライアントへ配信
                        Parallel.ForEach(_clients, p => p.SendAsync(new ArraySegment<byte>(buff.Take(ret.Count).ToArray()), WebSocketMessageType.Text, true, CancellationToken.None));
                    }
                    else if (ret.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("{0}:Session Close:{1}", DateTime.Now.ToString(), context.Request.RemoteEndPoint.Address.ToString());
                        break;
                    }
                }
                catch
                {
                    /// 例外 クライアントが異常終了しやがった
                    Console.WriteLine("{0}:Session Abort:{1}", DateTime.Now.ToString(), context.Request.RemoteEndPoint.Address.ToString());
                    break;
                }
            }

            /// クライアントを除外する
            _clients.Remove(ws);
            ws.Dispose();
        }
    }
}
