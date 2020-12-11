using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using AxShockwaveFlashObjects;
using System.Windows.Resources;
using Application = System.Windows.Application;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace RunningFred
{
    public partial class RunningFred : Form
    {
        public RunningFred( )
        {
            InitializeComponent( );
        }

        [ DllImport( "winmm.dll" ) ]
        private static extern int waveOutSetVolume( IntPtr hwo, uint dwVolume );

        public static void SetVolume(int volume)
        {
            int NewVolume = ( ( ushort.MaxValue / 10 ) * volume );
            uint NewVolumeAllChannels = ( ( ( uint )NewVolume & 0x0000ffff ) | ( ( uint )NewVolume << 16 ) );
            waveOutSetVolume( IntPtr.Zero, NewVolumeAllChannels );
        }

        [ DllImport( "gdi32.dll" ) ]
        static extern int GetDeviceCaps( IntPtr hdc, int nIndex );
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }

        // https://stackoverflow.com/questions/5977445/how-to-get-windows-display-settings/21450169#21450169
        private float GetScalingFactor( )
        {
            Graphics g = Graphics.FromHwnd( IntPtr.Zero );
            IntPtr desktop = g.GetHdc( );
            int LogicalScreenHeight = GetDeviceCaps(desktop, ( int )DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, ( int )DeviceCap.DESKTOPVERTRES);

            float ScreenScalingFactor = ( float )PhysicalScreenHeight / ( float )LogicalScreenHeight;

            return ScreenScalingFactor;
        }

        // Credits to someone, if you are hte creator and wish to be credited then contact me
        public static void LoadFlash( AxShockwaveFlash FlashObj, string Path )
        {
            StreamResourceInfo resource = Application.GetResourceStream( new Uri( Path, UriKind.Relative ) );

            using ( resource.Stream )
            {
                if ( resource.ContentType != "application/x-shockwave-flash" )
                {
                    throw new FileFormatException( "Path must be a valid swf." );
                }

                byte[ ] data = new byte[ resource.Stream.Length ];
                resource.Stream.Read( data, 0, ( int )resource.Stream.Length );
                LoadFlash( FlashObj, data );
            }
        }

        public static void LoadFlash( AxShockwaveFlash FlashObj, byte[ ] SwfFile )
        {
            using ( MemoryStream stream = new MemoryStream( ) )
            using ( BinaryWriter writer = new BinaryWriter( stream ) )
            {
                writer.Write( 8 + SwfFile.Length );
                writer.Write( 0x55665566 ); // 'fUfU'
                writer.Write( SwfFile.Length );
                writer.Write( SwfFile );
                stream.Seek( 0, SeekOrigin.Begin );
                FlashObj.OcxState = new AxShockwaveFlash.State( stream, 1, false, null );
            }
        }

        private void Form1_Load( object sender, EventArgs e )
        {
            // Mute the horrid loud game volume
            SetVolume( 0 );

            // Get the primary monitor resolution
            float flScalingFactor = GetScalingFactor( );
            Rectangle resolution = Screen.PrimaryScreen.Bounds;
            this.Size = new Size( ( int )( ( float )resolution.Width * flScalingFactor ), ( int )( ( float )resolution.Height * flScalingFactor ) );

            // Setup the bar at the bottom of the screen
            VerifLabel.Location = pictureBox1.Location = new Point( 0, resolution.Height - 20 );

            // Load and play the embedded flash file
            LoadFlash( axShockwaveFlash1, "runningfred.swf");
            axShockwaveFlash1.SetVariable( "security_flash", "1" );
            axShockwaveFlash1.Size = new Size( resolution.Width, resolution.Height );
            axShockwaveFlash1.WMode = "direct";
            axShockwaveFlash1.Quality = 1;
            axShockwaveFlash1.Menu = false;
            axShockwaveFlash1.Play( );
        }
    }
}
