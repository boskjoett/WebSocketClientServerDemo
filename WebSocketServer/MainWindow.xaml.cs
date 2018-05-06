using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using SharedDataTypes;

namespace WebSocketServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpListener httpListener;
        private WebSocket webSocket;

        public MainWindow()
        {
            InitializeComponent();

            httpListener = new HttpListener();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(TextBoxPort.Text);

            // Add prefix for normal HTTP REST API calls
            httpListener.Prefixes.Add($"http://*:{port}/restapi/");

            // Add prefix for websocket connections
            httpListener.Prefixes.Add($"http://+:{port}/websocket/");

            httpListener.Start();

            LogMessage("Server started on port " + port);

            httpListener.BeginGetContext(new AsyncCallback(RequestReceived), null);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Server stopped");
        }

        private void RequestReceived(IAsyncResult result)
        {
            HttpListenerContext context = httpListener.EndGetContext(result);

            if (context.Request.IsWebSocketRequest)
            {
                LogMessage("Websocket request received");
                HandleWebSocketRequest(context);
            }
            else
            {
                LogMessage("HTTP request received");
                HandleHttpRequest(context);
            }

            // Begin a new asynchronous (non-blocking) call to receive incoming client requests
            httpListener.BeginGetContext(new AsyncCallback(RequestReceived), null);
        }

        private async void HandleWebSocketRequest(HttpListenerContext context)
        {
            // Accept web socket connection and set keep-alive interval to 10 seconds
            HttpListenerWebSocketContext wc = await context.AcceptWebSocketAsync(null, new TimeSpan(10000));

            webSocket = wc.WebSocket;

            // Start a worker thread that processes incoming requests until websocket is closed.
            ThreadPool.QueueUserWorkItem(waitState =>
            {
                try
                {
                    WebsocketMessageReceiver();
                }
                catch (Exception ex)
                {
                    LogMessage("WebsocketMessageReceiver exception: " + ex.Message);
                }
            });
        }

        private async void WebsocketMessageReceiver()
        {
            byte[] receiveBuffer = new byte[1024];

            while (webSocket.State == WebSocketState.Open)
            {
                bool validMessage = true;
                ArraySegment<byte> buffer = new ArraySegment<byte>(receiveBuffer);
                WebSocketReceiveResult receiveResult;

                using (var ms = new MemoryStream())
                {
                    do
                    {
                        try
                        {
                            receiveResult = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            // A websocket exception is thrown by ReceiveAsync if client terminates without closing the websocket.
                            LogMessage($"WebSocket.ReceiveAsync threw exception: {ex.Message}");
                            return;
                        }

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None).Wait();
                            LogMessage("Websocket closed by client");
                            return;
                        }

                        if (receiveResult.MessageType != WebSocketMessageType.Text)
                        {
                            validMessage = false;
                            break;
                        }

                        ms.Write(buffer.Array, buffer.Offset, receiveResult.Count);

                    } while (!receiveResult.EndOfMessage);

                    if (!validMessage)
                    {
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        OnWebsocketMessageReceived(reader.ReadToEnd());
                    }
                }
            }
        }

        private void OnWebsocketMessageReceived(string message)
        {
            try
            {
                // Deserialize message base class to determine message type.
                MessageBase request = JsonConvert.DeserializeObject<MessageBase>(message);
                if (request == null)
                {
                    LogMessage("OnWebsocketMessageReceived: Unable to deserialize base message");
                    return;
                }

                LogMessage("OnWebsocketMessageReceived: Message type: " + request.MessageType);

                switch (request.MessageType)
                {
                    case MessageType.AddSubscriberRequest:
                        AddSubscriberRequestMessage addSubscriberRequestMessage = JsonConvert.DeserializeObject<AddSubscriberRequestMessage>(message);
                        LogMessage("Subscriber name: " + addSubscriberRequestMessage.Name);

                        AddSubscriberResponseMessage response = new AddSubscriberResponseMessage
                        {
                            SubscriberId = addSubscriberRequestMessage.SubscriberId,
                            SubscriptionId = Guid.NewGuid(),
                            Success = true
                        };

                        // Send AddSubscriberResponse back to sender
                        string jsonText = JsonConvert.SerializeObject(response);
                        byte[] msg = Encoding.UTF8.GetBytes(jsonText);
                        webSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None).Wait(5000);
                        break;

                    default:
                        LogMessage("Unknown message type");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage("OnWebsocketMessageReceived exception: " + ex.Message);
            }
        }

        private void HandleHttpRequest(HttpListenerContext context)
        {
            Uri requestUri = context.Request.Url;

            // TODO: Handle request
            byte[] bytes = Encoding.UTF8.GetBytes("Hello world!");
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private void LogMessage(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
              new Action(() => {
                  ListBoxLog.Items.Add(message);
              }));
        }
    }
}
