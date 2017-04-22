//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;
    using System.Windows.Input;
    using System.IO.Ports;
    using System.Text.RegularExpressions;
    using System.Runtime.InteropServices;
    using Excel = Microsoft.Office.Interop.Excel;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        //Setup the exceel worksheet  
        Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
        Excel.Worksheet ws;
        Excel.Range range;

        Skeleton outputSkele;
        int snapTimes;
        List<List<Joint>> jointList = new List<List<Joint>>();
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        private bool streaming = false;

        //Setup port
        SerialPort port = new SerialPort("COM3", 9600);

        private string previousFilename = "";

        Stopwatch sw = new Stopwatch();
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {           
            InitializeComponent();

            //Allow universial key listening in form.
            EventManager.RegisterClassHandler(typeof(Window),Keyboard.KeyDownEvent, new KeyEventHandler(keyDown), true);

            //Configure the Exceel worksheet.
            xlApp.Visible = false;
            xlApp.DisplayAlerts = false;
            Excel.Workbook wb = xlApp.Workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);           
            ws = (Excel.Worksheet)wb.Worksheets[1];
            ws.Cells[1, 1] = "Pulse rate/width";
            ws.Cells[1, 2] = "Amplitude";
            ws.Cells[1, 3] = "Amplitude 2";
            ws.Cells[1, 4] = "Distance Wrist";
            ws.Cells[1, 5] = "Time";
            ws.Cells[1, 6] = "Distance Elbow";
            ws.Cells[1, 7] = "Direction Wrist";
            ws.Cells[1, 8] = "Direction Elbow";
            ws.Columns.AutoFit();
        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                //Check if required information is given.
                bool anyChecked = (bool)channel45.IsChecked || (bool)channel67.IsChecked || (bool)channel89.IsChecked;
                if (String.IsNullOrWhiteSpace(pulserw.Text) || String.IsNullOrWhiteSpace(amplitude.Text) || !anyChecked)
                {
                    MessageBox.Show("Some options were left unfilled.");
                    return;
                }
                if (!streaming)
                {
                    try
                    {
                        port.Open();
                    }
                    catch
                    {
                        MessageBox.Show("No port found.");
                        return;
                    }
                    sw.Reset();
                    sw.Start();
                    streaming = true;
                    startstop.Content = "Streaming Status: Running";
                    if ((bool)channel45.IsChecked)
                    {
                        port.Write("4c");
                        port.Write("5c");
                    }
                    if ((bool)channel67.IsChecked)
                    {
                        port.Write("6c");
                        port.Write("7c");
                    }
                    if ((bool)channel89.IsChecked)
                    {
                        port.Write("8c");
                        port.Write("9c");
                    }

                    int width = Int32.Parse(Regex.Replace(pulserw.Text, @"\s+", ""));
                    int rate = Int32.Parse(Regex.Replace(pulserw.Text, @"\s+", ""));
                    int amp = Int32.Parse(Regex.Replace(amplitude.Text, @"\s+", ""));
                    int amp2 = Int32.Parse(Regex.Replace(amplitude_Copy.Text, @"\s+", ""));
                    port.Write(width + "w");
                    port.Write(rate + "r");
                    port.Write(amp + "a");
                    port.Write(amp2 + "v");
                    port.Write(0 + "s");
                    

                }
                else
                {
                    streaming = false;
                    startstop.Content = "Streaming Status: Stopped";
                    port.Write(0 + "e");
                    port.Close();
                }

            }
            
            if(e.Key == Key.Oem5)
            {
                if (!previousFilename.Equals(filenameBox.Text.Replace(" ","")))
                {
                    ws.Cells.Clear();
                    ws.Cells[1, 1] = "Pulse rate/width";
                    ws.Cells[1, 2] = "Amplitude";
                    ws.Cells[1, 3] = "Amplitude 2";
                    ws.Cells[1, 4] = "Distance Wrist";
                    ws.Cells[1, 5] = "Time";
                    ws.Cells[1, 6] = "Distance Elbow";
                    ws.Cells[1, 7] = "Direction Wrist";
                    ws.Cells[1, 8] = "Direction Elbow";
                    previousFilename = filenameBox.Text.Replace(" ","");

                }
                if (String.IsNullOrEmpty(previousFilename))
                {
                    previousFilename = filenameBox.Text.Replace(" ","");
                }                             
                button_Click(null,null);
            }
                
        }


        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
           
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
            if (null != this.sensor)
            {
                this.sensor.Stop();               
            }

            xlApp.Quit();

        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);                                       
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            outputSkele = skeleton;        
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }
 
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (filenameBox.Text.Equals(""))
            {
                MessageBox.Show("Specify filename");
                return;
            }
            range = ws.UsedRange;
            Object[,] saRet = (System.Object[,])range.get_Value(System.Reflection.Missing.Value);
            long iRows = saRet.GetUpperBound(0);

            if (outputSkele == null)
            {
                label.Content = "No Skeleton";
                return;
            }
            List<Joint> currJoints = new List<Joint>();
            Joint elbowL = outputSkele.Joints[JointType.ElbowLeft];
            Joint elbowR = outputSkele.Joints[JointType.ElbowRight];
            Joint handL = outputSkele.Joints[JointType.HandLeft];
            Joint handR = outputSkele.Joints[JointType.HandRight];
            Joint shoulderL = outputSkele.Joints[JointType.ShoulderLeft];
            Joint shoulderR = outputSkele.Joints[JointType.ShoulderRight];
            Joint wristL = outputSkele.Joints[JointType.WristLeft];
            Joint wristR = outputSkele.Joints[JointType.WristRight];
            currJoints.Add(elbowL);
            currJoints.Add(elbowR);
            currJoints.Add(handL);
            currJoints.Add(handR);
            currJoints.Add(shoulderL);
            currJoints.Add(shoulderR);
            currJoints.Add(wristL);
            currJoints.Add(wristR);

            if (snapTimes == 0)
            {
                label.Content = "Skeleton Start Position Snapped!";
                jointList.Insert(0, currJoints);
                snapTimes++;
            }
            else
            {
                label.Content = "Skeleton End Position Snapped!";
                jointList.Insert(1, currJoints);
                snapTimes = 0;
            }

            if (snapTimes == 0)
            {
                sw.Stop();
                for (int i = 0; i < currJoints.Count; i++)
                {
                    if (jointList[0][i].JointType.Equals(JointType.WristRight))
                    {
                        float res = calcDist(jointList[0][i], jointList[1][i]);
                        List<float> direction = calcDirection(jointList[0][i], jointList[1][i]);
                        string directionStr = "<";
                        ws.Cells[iRows + 1, 1] = pulserw.Text;
                        ws.Cells[iRows + 1, 2] = amplitude.Text;
                        ws.Cells[iRows + 1, 3] = amplitude_Copy.Text;
                        ws.Cells[iRows + 1, 4] = res.ToString().Replace(",",".");
                        ws.Cells[iRows + 1, 5] = sw.Elapsed.Milliseconds.ToString();
                        foreach(float dir in direction)
                        {                            
                            directionStr += dir.ToString().Replace(",", ".").Substring(0, 5) + ";";
                        }
                        directionStr = directionStr.Substring(0, directionStr.Length-1);
                        directionStr += ">";
                        ws.Cells[iRows + 1, 7] = directionStr;
                    }
                    if (jointList[0][i].JointType.Equals(JointType.ElbowRight))
                    {
                        float res = calcDist(jointList[0][i], jointList[1][i]);
                        List<float> direction = calcDirection(jointList[0][i], jointList[1][i]);
                        ws.Cells[iRows + 1, 6] = res.ToString().Replace(",", ".");
                        string directionStr = "<";
                        foreach (float dir in direction)
                        {
                            directionStr += dir.ToString().Replace(",", ".").Substring(0, 5) + ";";
                        }
                        directionStr = directionStr.Substring(0, directionStr.Length - 1);
                        directionStr += ">";
                        ws.Cells[iRows + 1, 8] = directionStr;
                    }
                }
            }
            ws.Columns.AutoFit();
            ws.SaveAs("C:\\Users\\Benjamin\\Dropbox\\Speciale\\Rapport\\Study3\\Results\\"+filenameBox.Text.Replace(" ","")+".xlsx");
        }

        //Calculate distance between two joints
        private float calcDist(Joint first,Joint second)
        {
            float deltaX = first.Position.X - second.Position.X;
            float deltaY = first.Position.Y - second.Position.Y;
            float deltaZ = first.Position.Z - second.Position.Z;

            float res = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);                    
            return res * 100.0f;
        }

        private List<float> calcDirection(Joint first, Joint second)
        {
            List<float> direction = new List<float>();
            direction.Add((first.Position.X - second.Position.X) * 100.0f);
            direction.Add((first.Position.Y - second.Position.Y) * 100.0f);
            direction.Add((first.Position.Z - second.Position.Z) * 100.0f);
            return direction;                
        }
        private void filenameBox_KeyDown(object sender, KeyEventArgs e)
        {            
            if(e.Key == Key.Oem5)
            {
                e.Handled = true;
                button.Focus();
            }
            
        }
    }
}