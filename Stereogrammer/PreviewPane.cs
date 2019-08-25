using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Stereogrammer
{
    /// <summary>
    /// Now I'm sure I should be doing this as a custom control that I can drop into WPF Designer and use cunning data bindings on...
    /// but I'm also sure I don't know how to do it, and for now it's a lot easier to do it old-school.
    /// </summary>
    class PreviewPane
    {
        public enum PreviewTypes { NONE, DEPTHMAP, TEXTURE, STEREOGRAM, PREVIEW };

        public IThumbnailable PreviewItem { get; set; }

        public PreviewTypes PreviewType { get; set; }

        MainWindow window { get; set; }
        Image imageView2D { get; set; }
        Image imageOverlay { get; set; }
        Grid grid { get; set; }


        /// <summary>
        /// This is pretty disgusting... oh well, get it working then figure out the 'good' way to do it
        /// </summary>
        /// <param name="window"></param>
        public PreviewPane( MainWindow window  )
        {
            this.grid = window.gridMainPanel;
            imageView2D = window.image2DView;
            imageOverlay = window.imageOverlay;
            PreviewType = PreviewTypes.NONE;
        }

        /// <summary>
        /// Set a specific preview item
        /// </summary>
        /// <param name="item"></param>
        public void SetPreviewItem( IThumbnailable item )
        {
            PreviewType = PreviewTypes.PREVIEW;
            PreviewItem = item;
            Update();
        }

        /// <summary>
        /// Set a specific preview item
        /// </summary>
        /// <param name="item"></param>
        public void SetPreviewType( PreviewTypes type )
        {
            switch (type)
	        {
		        case PreviewTypes.NONE:
                case PreviewTypes.DEPTHMAP:
                case PreviewTypes.TEXTURE:
                case PreviewTypes.STEREOGRAM:
                    PreviewType = type;
                    Update();
                    break;
                case PreviewTypes.PREVIEW:
                default:
                    throw new Exception( String.Format( "Cannot set {0} as a preview type", type.ToString() ) );
	        }
        }


        /// <summary>
        /// Update the preview pane... should do this whenever the item or type changes
        /// </summary>
        /// <param name="window"></param>
        void Update()
        {
            switch ( PreviewType )
            {
                case PreviewTypes.NONE:
                case PreviewTypes.PREVIEW:
                    break;

                case PreviewTypes.DEPTHMAP:
                    if ( window.SelectedDepthmap != PreviewItem )
                        PreviewItem = window.SelectedDepthmap;
                    break;

                case PreviewTypes.TEXTURE:
                    if ( window.SelectedTexture != null && window.SelectedTexture != PreviewItem )
                        PreviewItem = window.SelectedTexture;
                    break;

                case PreviewTypes.STEREOGRAM:
                    if ( window.SelectedStereogram != null && window.SelectedStereogram != PreviewItem )
                        PreviewItem = window.SelectedStereogram;
                    break;

                default:
                    throw new Exception( "Unknown preview type" );
            }

            if ( PreviewItem != null )
            {
                imageView2D.Source = PreviewItem.GetBitmap();
            }
        }

    }
}
