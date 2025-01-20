using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace UndertaleModToolAvalonia
{
    static class WindowLoader
    {
        private static readonly List<dynamic> views = new List<dynamic>();
        public static void setMainWindow(Window window)
        {
            try
            {
                if (views.Find(name => name.Equals(window)) == null)
                    views.Add(window);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static async System.Threading.Tasks.Task<Window> createWindowAsync(Window perent, Type window, bool dialogBox = false, params object[] parms)
        {
            try
            {
                dynamic temp = views.Find(name => name.GetType().Equals(window));
                if (temp == null)
                {
                    if (parms != null)
                        temp = Activator.CreateInstance(window, parms);
                    else
                        temp = Activator.CreateInstance(window);
                    views.Add(temp);
                }
                ((Window)temp).Closed += onClose;
                if (dialogBox)
                    await temp.ShowDialog(perent);
                else
                    temp.Show();
                temp.Focus();
                return temp;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static async System.Threading.Tasks.Task<Window> createWindowAsync(Window perent, Type window, Type windowViewModel, bool isWindowInstanceRequired = false, bool isDialogBox = false, params object[] parms)
        {
            try
            {
                Window temp = await createWindowAsync(perent, window, isDialogBox);
                List<object> args = new List<object>();
                if (isWindowInstanceRequired)
                    args.Add(temp);
                args.AddRange(parms);
                temp.DataContext = Activator.CreateInstance(windowViewModel, args.ToArray());
                return temp;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + "\nStack Trace: \n" + e.StackTrace);
            }
        }

        public static Window createWindow(Window perent, Type window, bool dialogBox = false, params object[] parms)
        {
            try
            {
                dynamic temp = views.Find(name => name.GetType().Equals(window));
                if (temp == null)
                {
                    if (parms != null)
                        temp = Activator.CreateInstance(window, parms);
                    else
                        temp = Activator.CreateInstance(window);
                    views.Add(temp);
                    ((Window)temp).Closed += onClose;
                    if (dialogBox)
                        temp.ShowDialog(perent);
                    else
                        temp.Show();
                    temp.Focus();
                }
                return temp;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static Window createWindow(Window perent, Type window, Type windowViewModel, bool isWindowInstanceRequired = false, bool isDialogBox = false, params object[] parms)
        {
            try
            {
                Window temp = createWindow(perent, window, isDialogBox);
                List<object> args = new List<object>();
                if (isWindowInstanceRequired)
                    args.Add(temp);
                args.AddRange(parms);
                temp.DataContext = Activator.CreateInstance(windowViewModel, args.ToArray());
                return temp;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static UserControl createView(UserControl perent, Type view, params object[] parms)
        {
            try
            {
                dynamic temp = views.Find(name => name.GetType().Equals(view));
                if (temp == null)
                {
                    if (parms != null)
                        temp = Activator.CreateInstance(view, parms);
                    else
                        temp = Activator.CreateInstance(view);
                    views.Add(temp);
                    temp.Focus();
                }
                return temp;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }


        public static Control createView(UserControl perent, Type view, Type ViewModel, bool isWindowInstanceRequired = false, params object[] parms)
        {
            try
            {
                UserControl temp = createView(perent, view);
                List<object> args = new List<object>();
                if (isWindowInstanceRequired)
                    args.Add(temp);
                args.AddRange(parms);
                try
                {
                    temp.DataContext = Activator.CreateInstance(ViewModel, args.ToArray());
                }
                catch (System.Exception e)
                {
                    return new TextBlock { Text = "Not Found: \n" + e.Message };
                }
                return temp;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static void onClose(object sender, EventArgs e)
        {
            try
            {
                views.Remove((Window)sender);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static Window findWindow(Type type)
        {
            foreach (Window w in views)
            {
                if (w.GetType() == type)
                    return w;
            }
            return null;
        }
    }
}
