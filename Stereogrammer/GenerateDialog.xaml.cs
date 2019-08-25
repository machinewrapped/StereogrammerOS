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

using Stereogrammer.Model;

namespace Stereogrammer
{
    /// <summary>
    /// Interaction logic for SaveDialog.xaml
    /// </summary>
    public partial class DialogGenerateStereogram : Window
    {
        public Options generateOptions = null;
        public List<BitmapType> depthmaps = null;
        public List<BitmapType> textures = null;

        public bool SaveStereogram = false;

        public DialogGenerateStereogram( Options options, List<BitmapType> depthmaps, List<BitmapType> textures, bool SaveIsDefault )
        {
            InitializeComponent();
            generateOptions = options;
            this.depthmaps = depthmaps;
            this.textures = textures;
            this.DataContext = options;

            buttonSave.IsDefault = SaveIsDefault;
            buttonOK.IsDefault = !buttonSave.IsDefault;
        }

        private void GenerateDialog_Loaded( object sender, RoutedEventArgs e )
        {
            comboDepthmap.ItemsSource = depthmaps;
            comboDepthmap.SelectedItem = generateOptions.depthmap;
            comboTexture.ItemsSource = textures;
            comboTexture.SelectedItem = generateOptions.texture;
        }

        private void GenerateDialog_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
        }

        private void buttonSave_Click( object sender, RoutedEventArgs e )
        {
            this.DialogResult = true;
            this.SaveStereogram = true;
            generateOptions.depthmap = (Depthmap)comboDepthmap.SelectedItem;
            generateOptions.texture = (Texture)comboTexture.SelectedItem;
            Close();
        }

        private void buttonOK_Click( object sender, RoutedEventArgs e )
        {
            this.DialogResult = true;
            this.SaveStereogram = false;
            generateOptions.depthmap = (Depthmap)comboDepthmap.SelectedItem;
            generateOptions.texture = (Texture)comboTexture.SelectedItem;
            Close();
        }

        private void buttonCancel_Click( object sender, RoutedEventArgs e )
        {
            this.DialogResult = false;
            this.SaveStereogram = false;
            Close();
        }

        // Preserve aspect ratio if the option is selected
        private void textHeight_LostFocus( object sender, RoutedEventArgs e )
        {
            if ( generateOptions.bPreserveAspectRatio )
            {
                double ratio = (double)generateOptions.depthmap.PixelWidth / generateOptions.depthmap.PixelHeight;
                int resolutionY = Convert.ToInt32( this.textHeight.Text );
                int resolutionX = (int)( ratio * resolutionY );
                textWidth.Text = resolutionX.ToString();
                generateOptions.resolutionX = resolutionX;
            }
        }

        private void textWidth_LostFocus( object sender, RoutedEventArgs e )
        {
            if ( generateOptions.bPreserveAspectRatio )
            {
                double ratio = (double)generateOptions.depthmap.PixelHeight / generateOptions.depthmap.PixelWidth;
                int resolutionX = Convert.ToInt32( this.textWidth.Text );
                int resolutionY = (int)( ratio * resolutionX );
                textHeight.Text = resolutionY.ToString();
                generateOptions.resolutionY = resolutionY;
            }

        }

        private void checkBoxPreserveAspect_Checked( object sender, RoutedEventArgs e )
        {
            if ( generateOptions.bPreserveAspectRatio )
            {
                double ratio = (double)generateOptions.depthmap.PixelWidth / generateOptions.depthmap.PixelHeight;
                generateOptions.resolutionX = (int)( ratio * generateOptions.resolutionY );
                textWidth.Text = generateOptions.resolutionX.ToString();        // Shirley data-binding is supposed to make that happen automatically?
            }
        }

    }
}
