using System.Windows;
using System.Windows.Controls;
using Windows.Media.SpeechRecognition;

namespace Search.App;

/// <summary>The microphone in the empty tab's field: speak and the words are typed in (Windows' own speech recognition).</summary>
public partial class MainWindow
{
    SpeechRecognizer? speech;
    bool listening;

    async void Mic_Click(object sender, RoutedEventArgs e)
    {
        if (listening)
        {
            try { if (speech is not null) await speech.ContinuousRecognitionSession.CancelAsync(); } catch { }
            return;
        }
        var ask = Placeholder.Text; // what to put back when listening ends
        try
        {
            speech ??= new SpeechRecognizer();
            if (speech.Constraints.Count == 0)
            {
                speech.Constraints.Add(new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.WebSearch, "web"));
                var compiled = await speech.CompileConstraintsAsync();
                if (compiled.Status != SpeechRecognitionResultStatus.Success) { Said("Speech is not available here"); speech.Constraints.Clear(); return; }
                speech.HypothesisGenerated += (_, h) => Dispatcher.Invoke(() => { if (listening) BlankField.Text = h.Hypothesis.Text; });
            }
            listening = true;
            MicDoor.Foreground = (System.Windows.Media.Brush)FindResource("Ink");
            Placeholder.Text = "Listening…";
            BlankField.Focus();
            var result = await speech.RecognizeAsync();
            if (result.Status == SpeechRecognitionResultStatus.Success && !string.IsNullOrWhiteSpace(result.Text))
            {
                BlankField.Text = result.Text.TrimEnd('.', ' ');
                BlankField.CaretIndex = BlankField.Text.Length;
            }
        }
        catch (Exception ex) when ((uint)ex.HResult == 0x80045509)
        {
            // The privacy switch for online speech recognition is off.
            Said("Turn on Online speech recognition in Windows Settings › Privacy › Speech");
            App.Trace("dictation: online speech recognition is off");
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:privacy-speech") { UseShellExecute = true }); } catch { }
        }
        catch (Exception ex) { App.Log(ex); Said("No microphone could be used"); }
        finally
        {
            listening = false;
            MicDoor.SetResourceReference(Control.ForegroundProperty, "Row.Muted");
            if (Placeholder.Text == "Listening…") Placeholder.Text = ask;
        }

        void Said(string text) => Placeholder.Text = text;
    }
}

public partial class MainWindow
{
    bool askMode;

    void Mode_Click(object sender, RoutedEventArgs e) => SetMode((sender as FrameworkElement)?.Tag as string == "ask");

    /// <summary>Search or Ask in the empty tab's field: Ask sends the words to the AI site chosen in Settings, in a tab.</summary>
    void SetMode(bool ask)
    {
        askMode = ask;
        foreach (var (button, on) in new[] { (SearchMode, !ask), (AskMode, ask) })
        {
            if (on) { button.SetResourceReference(Control.BackgroundProperty, "Ground"); button.SetResourceReference(Control.ForegroundProperty, "Ink"); }
            else { button.Background = System.Windows.Media.Brushes.Transparent; button.SetResourceReference(Control.ForegroundProperty, "Row.Muted"); }
        }
        if (!listening) Placeholder.Text = ask ? "Ask a question" : "Enter a web address";
        BlankField.Focus();
    }
}
