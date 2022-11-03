using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace NiL.Hub
{
    internal class HubConnectionWorker
    {
        private Thread _thread;
        private bool _reconnectOnFail;

        public HubConnectionWorker(HubConnection connection, bool reconnectOnFail)
        {
            Connection = connection;
            _reconnectOnFail = reconnectOnFail;
        }

        public HubConnection Connection { get; }

        private void hubConnectionWorker()
        {
            List<Action> doAfter = new List<Action>();
            while (Connection.State != HubConnectionState.Disposed)
            {
                if (Connection.IsConnected)
                {

                    try
                    {
                        var bytesToRead = 0;
                        while (Connection.IsConnected)
                        {
                            var socket = Connection.Socket;

                            if ((DateTime.Now.Ticks - Connection.LastActivityTimestamp) >= TimeSpan.FromSeconds(15).Ticks)
                            {
                                Connection.SendPing();
                            }

                            socket.Poll(10000, SelectMode.SelectRead);

                            while (socket.Available > 2 || (bytesToRead > 0 && socket.Available > 0))
                            {
                                try
                                {
                                    var chunkSize = bytesToRead == 0 ? 2 : Math.Min(socket.Available, bytesToRead);
                                    var oldLen = Connection.InputBufferReader.BaseStream.Length;
                                    Connection.InputBufferReader.BaseStream.SetLength(oldLen + chunkSize);
                                    socket.Receive(Connection.InputBuffer.GetBuffer(), (int)oldLen, chunkSize, SocketFlags.None);

                                    if (bytesToRead == 0)
                                        bytesToRead = Connection.InputBufferReader.ReadUInt16(); // size of data
                                    else
                                        bytesToRead -= chunkSize;

                                    if (bytesToRead == 0)
                                    {
                                        doAfter.Clear();

                                        Connection.ProcessReceived(doAfter);

                                        if (doAfter.Count != 0)
                                        {
                                            for (var i = 0; i < doAfter.Count; i++)
                                                doAfter[i].Invoke();
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.Error.WriteLine(e);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Console.Error.WriteLine("HubConnectionWorker stoped. " + (_reconnectOnFail ? "Try to reconnect. " : "Reconnect disabled. ") + "(Remote endpoint: " + Connection.EndPoint + ")");
                    }
                }

                if (_reconnectOnFail)
                {
                    Thread.Sleep(10000);
                    try
                    {
                        Connection.Reconnect();
                        Console.WriteLine("Reconnected to " + Connection.EndPoint);
                    }
                    catch (SocketException)
                    {
                        Console.Error.WriteLine("Unable to reconnect to " + Connection.EndPoint);
                    }
                }
                else
                    break;
            }
        }

        public void StartWorker()
        {
            var thread = new Thread(hubConnectionWorker)
            {
                Name = "Workder for connection from \"" + Connection.LocalHub.Name + "\" (" + Connection.LocalHub.Id + ") to " + Connection.EndPoint
            };
            _thread = thread;
            thread.Start();
        }
    }
}
