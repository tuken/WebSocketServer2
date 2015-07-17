using Topshelf;

namespace WebSocketServer2
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                //ロジックがあるクラスへの参照
                x.Service<Server>(s =>
                {
                    s.ConstructUsing(name => new Server());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                //Windowsサービスの設定
                x.RunAsLocalSystem();
                x.SetDescription("Test WebSocket Server 2");
                x.SetDisplayName("WebSocketServer2");
                x.SetServiceName("WebSocketServer2");
            });
        }
    }
}
