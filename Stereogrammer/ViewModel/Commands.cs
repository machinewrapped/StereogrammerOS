// Copyright 2012 Simon Booth
// All rights reserved
// http://machinewrapped.wordpress.com/stereogrammer/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using Stereogrammer.Model;

namespace Stereogrammer.ViewModel
{
    public class CommandView : RoutedCommand
    {
        public string LongName { get; private set; }
        public RoutedCommand Command { get { return this; } }

        public CommandView( string name )
        {
            LongName = name;
//            Command = command;
        }
    }

    public class Commands
    {
        public static CommandView CmdPreviewStereogram = new CommandView("Preview Stereogram");
        public static CommandView CmdGenerateStereogram = new CommandView("Generate Stereogram");
        public static CommandView CmdSaveStereogram = new CommandView("Save Stereogram");
        public static CommandView CmdRestoreStereogramSettings = new CommandView("Restore Settings");
        public static CommandView CmdRegenerateStereogram = new CommandView("Regenerate Stereogram");
        public static CommandView CmdFullscreen = new CommandView("View Fullscreen");
        public static CommandView CmdFullscreenClose = new CommandView("Close Fullscreen");
        public static CommandView CmdInvertDepthmap = new CommandView("Invert Depthmap");
        public static CommandView CmdMergeDepthmaps = new CommandView("Merge Depthmaps");
        public static CommandView CmdAdjustDepthmapLevels = new CommandView("Adjust Depthmap");
        public static CommandView CmdSaveDepthmap = new CommandView("Save Depthmap");
        public static CommandView CmdDefaultSettings = new CommandView("Restore Default Settings");

        public static CommandView CmdSelectItem = new CommandView("Select Item");
        public static CommandView CmdClearPalette = new CommandView("Clear Palette");
        public static CommandView CmdDeleteSelectedItems = new CommandView("Delete Selected Items");
        public static CommandView CmdSelectAndAddFiles = new CommandView("Select and Add Files");
        public static CommandView CmdDefaultResources = new CommandView("Default Resources");

        public static CommandView CmdAboutBox = new CommandView("About This Program");

        /// <summary>
        /// Get the list of commands supported by particular types of object, if any.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<CommandView> GetSupportedCommands( object type )
        {
            List<CommandView> supported = null;

            // Not nice having to do type-specific stuff here, but cleaner than having the bitmap types know what they support
            if ( type is Stereogram )
            {
                supported = new List<CommandView>() 
                    { Commands.CmdSaveStereogram, Commands.CmdRegenerateStereogram, Commands.CmdPreviewStereogram, Commands.CmdRestoreStereogramSettings, Commands.CmdFullscreen };
            }
            else if ( type is Depthmap )
            {
                supported = new List<CommandView>()
                    { Commands.CmdPreviewStereogram, Commands.CmdGenerateStereogram, Commands.CmdFullscreen, Commands.CmdInvertDepthmap, 
                        Commands.CmdAdjustDepthmapLevels, Commands.CmdMergeDepthmaps, Commands.CmdSaveDepthmap };
            }
            else if ( type is Texture )
            {
                supported = new List<CommandView>() { Commands.CmdPreviewStereogram, Commands.CmdGenerateStereogram, Commands.CmdFullscreen };
            }

            return supported;
        }
    
    }
}
