namespace Leaf.BreakReminder;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ReminderApplicationContext());
    }    
}