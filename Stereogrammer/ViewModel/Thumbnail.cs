// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;

using Stereogrammer.Model;


namespace Stereogrammer.ViewModel
{
    /// <summary>
    /// Helper for thumbnails in the palette
    /// </summary>
    public class Thumbnail : DependencyObject
    {
        public static readonly DependencyProperty BorderBrushProperty =
              DependencyProperty.Register( "BorderBrush", typeof( Brush ), typeof( Thumbnail ) );

        public BitmapType ThumbnailOf { get; private set; }

        public bool CanRemove { get { return ThumbnailOf.bCanRemove; } set { ThumbnailOf.bCanRemove = value; } }

        public RoutedCommand OnDoubleClick { get; set; }

        /// <summary>
        /// Thumbnail has exclusive selection in this palette
        /// </summary>
        public bool Selected
        {
            get { return bSelected; }
            set
            {
                bSelected = value;
                SetBorderBrush();
                Source = ThumbnailOf.GetThumbnail( bSelected );
            }
        }

        /// <summary>
        /// Thumbnail is one of the selected thumbnails in this palette
        /// </summary>
        public bool MultiSelected
        {
            get { return bMultiselected; }
            set
            {
                bMultiselected = value;
                SetBorderBrush();
            }
        }

        public string Name
        {
            get { return ThumbnailOf.Name; }
        }

        public string Filename
        {
            get { return ThumbnailOf.filename; }
        }

        public string Description
        {
            get { return String.Format( "{0} ({1}x{2})", Name, ThumbnailOf.PixelWidth, ThumbnailOf.PixelHeight ); }
        }

        public override string ToString()
        {
            return Description;
        }

        public ImageSource Source { get; private set; }

        public Brush BorderBrush
        {
            get { return (Brush)GetValue( BorderBrushProperty ); }
            set { SetValue( BorderBrushProperty, value ); }
        }

        private bool bSelected = false;
        private bool bMultiselected = false;

        public Thumbnail( BitmapType represents )
            : base()
        {
            ThumbnailOf = represents;

            Source = ThumbnailOf.GetThumbnail( false );

//            ContextMenu = GetContextMenu();
        }

        /// <summary>
        /// Border colour based on selected status
        /// </summary>
        private void SetBorderBrush()
        {
            if ( bSelected )
                BorderBrush = new SolidColorBrush( Colors.Green );
            else if ( bMultiselected )
                BorderBrush = new SolidColorBrush( Colors.Blue );
            else
                BorderBrush = new SolidColorBrush( Colors.White );
        }

        private List<CommandView> _commands;
        public List<CommandView> SupportedCommands {
            get
            {
                if ( _commands == null )
                {
                    _commands = Commands.GetSupportedCommands( ThumbnailOf );
                    _commands.Add( Commands.CmdDeleteSelectedItems );
                }
                return _commands;
            }
        }

    }



}
