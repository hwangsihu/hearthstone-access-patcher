using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace HearthstoneAccessPatcher;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += HandleThreadException;
        AppDomain.CurrentDomain.UnhandledException += HandleDomainException;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static void HandleDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        LogException((Exception)e.ExceptionObject);
        Application.Exit();
    }

    private static void HandleThreadException(object sender, ThreadExceptionEventArgs e)
    {
        LogException(e.Exception);
        Application.Exit();
    }

    private static void LogException(Exception ex)
    {
        string filePath = Constants.ErrorLogFile;
        using (StreamWriter writer = new(filePath, false))
        {
            writer.WriteLine($"Date : {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
            Exception? ex_chain = ex;
            while (ex_chain != null)
            {
                writer.WriteLine();
                writer.WriteLine(ex_chain.GetType().FullName);
                writer.WriteLine($"Message: {ex_chain.Message}");
                writer.WriteLine($"TargetSite: {ex_chain.TargetSite}");
                writer.WriteLine($"StackTrace: {ex_chain.StackTrace}");
                if (ex_chain.Data.Count > 0)
                {
                    writer.WriteLine("Data:");
                    foreach (DictionaryEntry entry in ex_chain.Data)
                    {
                        writer.WriteLine($"  {entry.Key}: {entry.Value}");
                    }
                }
                ex_chain = ex_chain.InnerException;
            }
        }
        MessageBox.Show($"심각한 오류가 발생했습니다.\n{ex.Message}\n자세한 내용은 'errors.log' 파일을 확인해 주세요.", "심각한 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

}
