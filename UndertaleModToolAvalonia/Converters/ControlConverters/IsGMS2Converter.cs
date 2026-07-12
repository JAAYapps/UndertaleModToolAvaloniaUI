using System;
using System.Globalization;
using System.Net.Mime;
using System.Threading;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Rendering;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Converters.ControlConverters;

public class IsGMS2Converter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? par = parameter as string;
            bool invert = par == "invert";
            bool isGMS2 = false;

            if (value is UndertaleRoom.RoomEntryFlags flags)
            {
                if (par == "flags")
                {
                    if (AppConstants.Data != null && AppConstants.Data.IsGameMaker2())
                    {
                        if (AppConstants.Data.IsVersionAtLeast(2024, 13))
                        {
                            if (!flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13))
                            {
                                try
                                {
                                    Dispatcher.UIThread.Post(() => Application.Current?.ShowError("Room flags of GM 2024.13+ games must contain the \"IsGM2024_13\" flag, otherwise the game will crash when loading that room."));
                                }
                                catch { }
                            }

                            flags |= UndertaleRoom.RoomEntryFlags.IsGM2024_13;
                        }
                        else
                        {
                            if (!flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2))
                            {
                                try
                                {
                                    Dispatcher.UIThread.Post(() => Application.Current?.ShowError("Room flags of GMS 2+ games must contain the \"IsGMS2\" flag, otherwise the game will crash when loading that room."));
                                }
                                catch { }
                            }

                            flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                        }
                    }

                    return flags;
                }
                else
                {
                    isGMS2 = (flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13)) ^ invert;
                }
            }
            else if (value is bool vis)
            {
                return !vis;
            }

            return !isGMS2;
        }
        
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string par && par == "flags")
                return value;
            throw new NotSupportedException();
        }
    }