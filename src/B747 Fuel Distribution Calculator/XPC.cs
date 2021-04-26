//NOTICES:
//    Copyright (c) 2013-2018 United States Government as represented by the Administrator of the
//    National Aeronautics and Space Administration.  All Rights Reserved.
//
//  DISCLAIMERS
//    No Warranty: THE SUBJECT SOFTWARE IS PROVIDED "AS IS" WITHOUT ANY WARRANTY OF ANY KIND,
//    EITHER EXPRESSED, IMPLIED, OR STATUTORY, INCLUDING, BUT NOT LIMITED TO, ANY WARRANTY THAT THE
//    SUBJECT SOFTWARE WILL CONFORM TO SPECIFICATIONS, ANY IMPLIED WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE, OR FREEDOM FROM INFRINGEMENT, ANY WARRANTY THAT THE SUBJECT
//    SOFTWARE WILL BE ERROR FREE, OR ANY WARRANTY THAT DOCUMENTATION, IF PROVIDED, WILL CONFORM TO
//    THE SUBJECT SOFTWARE. THIS AGREEMENT DOES NOT, IN ANY MANNER, CONSTITUTE AN ENDORSEMENT BY
//    GOVERNMENT AGENCY OR ANY PRIOR RECIPIENT OF ANY RESULTS, RESULTING DESIGNS, HARDWARE,
//    SOFTWARE PRODUCTS OR ANY OTHER APPLICATIONS RESULTING FROM USE OF THE SUBJECT SOFTWARE.
//    FURTHER, GOVERNMENT AGENCY DISCLAIMS ALL WARRANTIES AND LIABILITIES REGARDING THIRD-PARTY
//    SOFTWARE, IF PRESENT IN THE ORIGINAL SOFTWARE, AND DISTRIBUTES IT "AS IS."
//
//    Waiver and Indemnity:  RECIPIENT AGREES TO WAIVE ANY AND ALL CLAIMS AGAINST THE UNITED STATES
//    GOVERNMENT, ITS CONTRACTORS AND SUBCONTRACTORS, AS WELL AS ANY PRIOR RECIPIENT. IF
//    RECIPIENT'S USE OF THE SUBJECT SOFTWARE RESULTS IN ANY LIABILITIES, DEMANDS, DAMAGES,
//    EXPENSES OR LOSSES ARISING FROM SUCH USE, INCLUDING ANY DAMAGES FROM PRODUCTS BASED ON, OR
//    RESULTING FROM, RECIPIENT'S USE OF THE SUBJECT SOFTWARE, RECIPIENT SHALL INDEMNIFY AND HOLD
//    HARMLESS THE UNITED STATES GOVERNMENT, ITS CONTRACTORS AND SUBCONTRACTORS, AS WELL AS ANY
//    PRIOR RECIPIENT, TO THE EXTENT PERMITTED BY LAW. RECIPIENT'S SOLE REMEDY FOR ANY SUCH MATTER
//    SHALL BE THE IMMEDIATE, UNILATERAL TERMINATION OF THIS AGREEMENT.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace B747_Fuel_Distribution_Calculator
{
    class XPlaneConnect : IDisposable
    {
        private UdpClient socket;
        private IPAddress xplaneAddr;
        private int xplanePort;
        private IPEndPoint xpEndPoint;

        /**
         * Gets the port on which the client sends data to X-Plane.
         *
         * @return The outgoing port number.
         */
        public int getXplanePort()
        {
            return xplanePort;
        }

        /**
         * Sets the port on which the client sends data to X-Plane
         *
         * @param port The new outgoing port number.
         * @throws ArgumentException If {@code port} is not a valid port number.
         */
        private void setXplanePort(int port)
        {
            if (port < 0 || port >= 0xFFFF)
            {
                throw new ArgumentException("Invalid port (must be non-negative and less than 65536).");
            }
            xplanePort = port;
        }

        /**
         * Gets the hostname of the X-Plane host.
         *
         * @return The hostname of the X-Plane host.
         */
        public IPAddress getXplaneAddr()
        {
            return xplaneAddr;
        }

        /**
         * Sets the hostname of the X-Plane host.
         *
         * @param host The new hostname of the X-Plane host machine.
         * @throws UnknownHostException {@code host} is not valid.
         */
        private void setXplaneAddr(string host)
        {
            if (!IPAddress.TryParse(host, out xplaneAddr))
            {
                IPAddress[] addresses = Dns.GetHostEntry(host).AddressList;
                if (addresses.Length > 0)
                {
                    xplaneAddr = addresses[0];
                }
                else
                {
                    throw new ArgumentException("Invalid host provided.");
                }
            }
        }

        /**
         * Initializes a new instance of the {@code XPlaneConnect} class using default ports and assuming X-Plane is running on the
         * local machine.
         *
         * @throws SocketException If this instance is unable to bind to the default receive port.
         */
        public XPlaneConnect() : this(IPAddress.Loopback, 49009, 0, 100) { }

        /**
         * Initializes a new instance of the {@code XPlaneConnect} class with the specified timeout using default ports and
         * assuming X-Plane is running on the local machine.
         *
         * @param timeout The time, in milliseconds, after which read attempts will timeout.
         * @throws SocketException If this instance is unable to bind to the default receive port.
         */
        public XPlaneConnect(int timeout) : this(IPAddress.Loopback, 49009, 0, timeout) { }

        /**
         * Initializes a new instance of the {@code XPlaneConnect} class using the specified ports and X-Plane host.
         *
         * @param xpHost The network host on which X-Plane is running.
         * @param xpPort The port on which the X-Plane Connect plugin is listening.
         * @param port   The local port to use when sending and receiving data from XPC.
         * @throws java.net.SocketException      If this instance is unable to bind to the specified port.
         * @throws java.net.UnknownHostException If the specified hostname can not be resolved.
         */
        public XPlaneConnect(String xpHost, int xpPort, int port) : this(xpHost, xpPort, port, 100) { }

        /**
         * Initializes a new instance of the {@code XPlaneConnect} class using the specified ports, hostname, and timeout.
         *
         * @param xpHost The network host on which X-Plane is running.
         * @param xpPort The port on which the X-Plane Connect plugin is listening.
         * @param port   The port on which the X-Plane Connect plugin is sending data.
         * @param timeout    The time, in milliseconds, after which read attempts will timeout.
         * @throws java.net.SocketException      If this instance is unable to bind to the specified port.
         * @throws java.net.UnknownHostException If the specified hostname can not be resolved.
         */
        public XPlaneConnect(String xpHost, int xpPort, int port, int timeout)
        {
            this.socket = new UdpClient(port);
            setXplaneAddr(xpHost);
            setXplanePort(xpPort);
            xpEndPoint = new IPEndPoint(this.xplaneAddr, this.xplanePort);
            this.socket.Client.ReceiveTimeout = timeout;
        }

        public XPlaneConnect(IPAddress xpHost, int xpPort, int port, int timeout)
        {
            this.socket = new UdpClient(port);
            this.xplaneAddr = xpHost;
            setXplanePort(xpPort);
            xpEndPoint = new IPEndPoint(this.xplaneAddr, this.xplanePort);
            this.socket.Client.ReceiveTimeout = timeout;
        }

        /**
         * Gets the port on which the client receives data from the plugin.
         *
         * @return The incoming port number.
         */
        public int getRecvPort() { return ((IPEndPoint)socket.Client.LocalEndPoint).Port; }

        /**
         * Closes the underlying socket.
         */
        public void Dispose()
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
                socket = null;
            }
        }

        /**
         * Requests a single dref value from X-Plane.
         *
         * @param dref The name of the dref requested.
         * @return     A byte array representing data dependent on the dref requested.
         * @throws IOException If either the request or the response fails.
         */
        public float[] getDREF(String dref)
        {
            return getDREFs(new String[] { dref })[0];
        }

        /**
         * Requests several dref values from X-Plane.
         *
         * @param drefs An array of dref names to request.
         * @return      A multidimensional array representing the data for each requested dref.
         * @throws IOException If either the request or the response fails.
         */
        public float[][] getDREFs(String[] drefs)
        {
            //Preconditions
            if (drefs == null || drefs.Length == 0)
            {
                throw new ArgumentException("drefs must be a valid array with at least one dref.");
            }
            if (drefs.Length > 255)
            {
                throw new ArgumentException("Can not request more than 255 DREFs at once.");
            }

            //Convert drefs to bytes.
            byte[][] drefBytes = new byte[drefs.Length][];

            for (int i = 0; i < drefs.Length; ++i)
            {
                drefBytes[i] = Encoding.UTF8.GetBytes(drefs[i]);
                if (drefBytes[i].Length == 0)
                {
                    throw new ArgumentException("DREF " + i + " is an empty string!");
                }
                if (drefBytes[i].Length > 255)
                {
                    throw new ArgumentException("DREF " + i + " is too long (must be less than 255 bytes in UTF-8). Are you sure this is a valid DREF?");
                }
            }

            //Build and send message
            MemoryStream os = new MemoryStream();
            byte[] tempBytes = Encoding.UTF8.GetBytes("GETD");
            os.Write(tempBytes, 0, tempBytes.Length);
            os.WriteByte(0xF); //Placeholder for message length
            os.WriteByte((byte)drefs.Length);
            foreach (byte[] dref in drefBytes)
            {
                os.WriteByte((byte)dref.Length);
                os.Write(dref, 0, dref.Length);
            }
            tempBytes = os.ToArray();
            socket.Send(tempBytes, tempBytes.Length, this.xpEndPoint);

            //Read response
            byte[] data = socket.Receive(ref xpEndPoint);
            if (data.Length == 0)
            {
                throw new IOException("No response received.");
            }
            if (data.Length < 6)
            {
                throw new IOException("Response too short");
            }
            float[][] result = new float[drefs.Length][];

            MemoryStream inStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(inStream))
            {
                writer.Write(data);
            }
            tempBytes = inStream.ToArray();
            int cur = 6;
            for (int j = 0; j < result.Length; ++j)
            {
                result[j] = new float[data[cur++]];
                for (int k = 0; k < result[j].Length; ++k) //TODO: There must be a better way to do this
                {
                    result[j][k] = System.BitConverter.ToSingle(tempBytes, cur);
                    cur += 4;
                }
            }
            return result;
        }

        /**
         * Sends a command to X-Plane that sets the given DREF.
         *
         * @param dref  The name of the X-Plane dataref to set.
         * @param value An array of floating point values whose structure depends on the dref specified.
         * @throws IOException If the command cannot be sent.
         */
        public void sendDREF(string dref, float[] value)
        {
            sendDREFs(new String[] { dref }, new float[][] { value });
        }

        /**
         * Sends a command to X-Plane that sets the given DREF.
         *
         * @param drefs  The names of the X-Plane datarefs to set.
         * @param values A sequence of arrays of floating point values whose structure depends on the drefs specified.
         * @throws IOException If the command cannot be sent.
         */
        public void sendDREFs(string[] drefs, float[][] values)
        {
            //Preconditions
            if (drefs == null || drefs.Length == 0)
            {
                throw new ArgumentException(("drefs must be non-empty."));
            }
            if (values == null || values.Length != drefs.Length)
            {
                throw new ArgumentException("values must be of the same size as drefs.");
            }

            MemoryStream os = new MemoryStream();
            byte[] tempBytes = Encoding.UTF8.GetBytes("DREF");
            os.Write(tempBytes, 0, tempBytes.Length);
            os.WriteByte(0xFF); //Placeholder for message length
            for (int i = 0; i < drefs.Length; ++i)
            {
                String dref = drefs[i];
                float[] value = values[i];

                if (dref == null)
                {
                    throw new ArgumentException("dref must be a valid string.");
                }
                if (value == null || value.Length == 0)
                {
                    throw new ArgumentException("value must be non-null and should contain at least one value.");
                }

                //Convert drefs to bytes.
                byte[] drefBytes = Encoding.UTF8.GetBytes(dref);
                if (drefBytes.Length == 0)
                {
                    throw new ArgumentException("DREF is an empty string!");
                }
                if (drefBytes.Length > 255)
                {
                    throw new ArgumentException("dref must be less than 255 bytes in UTF-8. Are you sure this is a valid dref?");
                }

                MemoryStream bb = new MemoryStream();
                using (BinaryWriter writer = new BinaryWriter(bb))
                {
                    for (int j = 0; j < value.Length; ++j)
                    {
                        writer.Write(value[j]);
                    }
                }
                tempBytes = bb.ToArray();

                //Build and send message
                os.WriteByte((byte)drefBytes.Length);
                os.Write(drefBytes, 0, drefBytes.Length);
                os.WriteByte((byte)value.Length);
                os.Write(tempBytes, 0, tempBytes.Length);
            }
            byte[] osBytesArray = os.ToArray();
            socket.Send(osBytesArray, osBytesArray.Length, xpEndPoint);
        }
    }
}

