// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;

using System.Reflection;
using System.Diagnostics;

namespace Stereogrammer
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox( string[] about )
        {
            InitializeComponent();

            version.Content = String.Format( "Version {0}", GetRunningVersion() );
            this.about.Text = String.Join( Environment.NewLine, about );

            // Embed some hyperlinks
            Hyperlink[] links = new Hyperlink[] {
                new Hyperlink( new Run( "Homepage" ) ) { NavigateUri = new Uri( @"http://machinewrapped.wordpress.com/stereogrammer/" ) },
                new Hyperlink( new Run( "Get More Textures @ InfiniteFish" ) ) { NavigateUri = new Uri( @"http://infinitefish.com/textures/" ) },
                new Hyperlink( new Run( "Stereograms @ Techmind" ) ) { NavigateUri = new Uri( @"http://www.techmind.org/stereo/stereo.html" ) }
            };

            foreach (var link in links)
	        {
                link.RequestNavigate += new RequestNavigateEventHandler( delegate( object sender, RequestNavigateEventArgs e )
                {
                    Process.Start( new ProcessStartInfo( e.Uri.AbsoluteUri ) );
                    e.Handled = true;
                } );
		 
                stackPanel1.Children.Add( new Label() { HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center, Content = link } );
	        }
        }

        // Thanks to Ed Haber @ Stack Overflow
        private Version GetRunningVersion()
        {            
            try
            {
                return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }


        private void buttonOK_Click_1( object sender, RoutedEventArgs e )
        {
            Close();
        }
    }
}
