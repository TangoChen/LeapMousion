using System;
using System.Threading;
using Leap;
using System.Runtime.InteropServices;
using System.Collections;

namespace LeapMousion
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Title = "Leap Mousion v2.0";
            Console.WriteLine("Leap Mousion v2.0 by TangoChen    :)\r\n------------------------------------\r\nBlog: TangoChen.com\r\nYoutube Channel: www.youtube.com/tan9ochen\r\n--------------------\r\nTwitter:\ttwitter.com/tangochen\t@TangoChen\r\n微博(Weibo):\tweibo.com/tangochen\t@TangoChen\r\n--------------------\r\nPress any key to exit...\r\n--------------------");
            
            // Create a sample listener and controller
            SampleListener listener = new SampleListener();
            Controller controller = new Controller();

            foreach (string arg in args)
            {
                switch (arg.Substring(0, 1).ToLower())
                {
                    case "l": // L = Set isLeftHanded = true;
                        listener.isLeftHanded = true;
                        break;
                    case "s": // Scale
                        float.TryParse(arg.Substring(1), out listener.moveScale);
                        break;
                    case "r": // Reverse Orientation
                        /* 
                         * It won't just reverse the horizontal axes but also change the fingerIndex...
                         * So this is used when the device get reversed horizontal axes
                         * and you don't want to click the [Reverse Orientation] inside the [Leap Motion Controller Settings] or
                         * rotate the device...
                         */
                        listener.isReversed = true;
                        break;
                    case "c": // Click-Only
                        listener.isClickOnly = true;
                        break;
                }
            }
            
            Console.WriteLine(
                "Speed: " + listener.moveScale.ToString() +
                "\r\nClick-Only: " + listener.isClickOnly.ToString() + 
                "\r\nReversed: " + listener.isReversed.ToString() + 
                "\r\nLeft Handed Mode: " + (listener.isLeftHanded ? "Enabled" : "Disabled") +
                "\r\n--------------------"
                );

            // Receive frames of tracking data while in the background
            controller.SetPolicyFlags(Controller.PolicyFlag.POLICYBACKGROUNDFRAMES);

            // Have the sample listener receive events from the controller
            controller.AddListener(listener);
            
            // Keep this process running until any key is pressed
            Console.ReadKey(true); //System.Diagnostics.Process.Start("pause"); Won't work...

            // Remove the sample listener when done
            controller.RemoveListener(listener);
            controller.Dispose();

        }

    }

    class SampleListener : Listener
    {
        public float moveScale = 1f;
        public bool isReversed = false;
        public bool isLeftHanded = false;
        public bool isClickOnly = false;

        private Object thisLock = new Object();
        bool isMouseDown = false;

        int screenWidth = (int)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = (int)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

        private void SafeWriteLine(String line)
        {
            lock (thisLock)
            {
                Console.WriteLine(line);
            }
        }

        public override void OnInit(Controller controller)
        {
            SafeWriteLine("Initialized... :|");
        }

        public override void OnConnect(Controller controller)
        {
            SafeWriteLine("Connected... :D");
        }

        public override void OnDisconnect(Controller controller)
        {
            SafeWriteLine("Disconnected... :(");
        }

        public override void OnFrame(Controller controller)
        {
            Frame frame = controller.Frame();
            HandList hands = frame.Hands;
            
            //Console.Clear();

            foreach (Hand hand in hands)
            {

                // Ignore other tracked objects that are out of the proper area
                if (Math.Abs(hand.PalmPosition.z) > 150) continue;

                // Check if the hand has any fingers
                FingerList fingers = hand.Fingers;

                //SafeWriteLine(frame.Timestamp.ToString() + " Hands: " + hands.Count.ToString() + ", Fingers: " + fingers.Count.ToString()+", Tools: " + tools.Count.ToString() );

                if (!fingers.Empty)
                {
                    Vector fingerPos;

                    if (fingers.Count > 1)
                    {
                        // fingerIndex is used to determine which finger's position is used to determine the mouse position... lol

                        // Default: Right Handed: Get the right finger tip
                        int fingerIndex = (fingers[0].TipPosition.x < fingers[1].TipPosition.x) ? 1 : 0;
                        // Left Handed: Get the left finger tip
                        if (isLeftHanded ^ isReversed) fingerIndex = 1 - fingerIndex;
                        /*
                         * If you wanna remove the "isReversed" feature, replace the code above with the following one:
                         * if (isLeftHanded) fingerIndex = 1 - fingerIndex;
                         */
                        fingerPos = fingers[fingerIndex].TipPosition;
                        
                        if (isMouseDown)
                        {
                            if(!isClickOnly) MouseLeftUp();
                            isMouseDown = false;
                        }

                    }
                    else // Only one finger is tracked
                    {
                        if (!isMouseDown)
                        {
                            if (isClickOnly)
                            {
                                MouseLeftClick();
                            }
                            else
                            {
                                MouseLeftDown();
                            }
                            
                            isMouseDown = true;
                        }

                        fingerPos = fingers[0].TipPosition;
                        
                    }

                    /*
                     * How to get values like -60, 60 ?
                     * Use SafeWriteLine to output the finger tip's position..
                     * and move the finger tip to the leftmost and rightmost sides you want...
                     * then you can decide how wide the x is...
                     */
                    //Device x = -60 ～ 60
                    float cursorX = (fingerPos.x + 60) / 120 * screenWidth * moveScale * (isReversed ? -1 : 1);
                    //Device y = 110 ～ 170
                    float cursorY = (1 - (fingerPos.y - 110) / 70) * screenHeight * moveScale;

                    SetCursorPos((int)cursorX, (int)cursorY);
                }

                break;

            }

        }

        #region Mouse Control

        //Get Position
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);

        //Move Mouse
        //SetCursorPos(10, 10);
        
        public void MouseLeftClick()
        {
            //mouse_event(MouseEventFlag.LeftDown | MouseEventFlag.Absolute, 0, 0, 0, UIntPtr.Zero);
            //Thread.Sleep(50);
            //mouse_event(MouseEventFlag.LeftUp | MouseEventFlag.Absolute, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventFlag.LeftDown | MouseEventFlag.LeftUp | MouseEventFlag.Absolute, 0, 0, 0, UIntPtr.Zero);
        }
        
        public void MouseLeftDown()
        {
            mouse_event(MouseEventFlag.LeftDown, 0, 0, 0, UIntPtr.Zero);
        }

        public void MouseLeftUp()
        {
            mouse_event(MouseEventFlag.LeftUp, 0, 0, 0, UIntPtr.Zero);
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        static extern void mouse_event(MouseEventFlag flags, int dx, int dy, uint data, UIntPtr extraInfo);
        [Flags]
        enum MouseEventFlag : uint
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            XDown = 0x0080,
            XUp = 0x0100,
            Wheel = 0x0800,
            VirtualDesk = 0x4000,
            Absolute = 0x8000
        }
        #endregion

    }
}