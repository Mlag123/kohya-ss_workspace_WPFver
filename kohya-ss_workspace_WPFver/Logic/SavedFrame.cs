namespace kohya_ss_workspace_WPFver.Logic;

public class SavedFrame
{
    
    
    public string ImageName { get; set; }
    public int nWidth { get; set; }
    public int nHeight { get; set; }
    public double Seconds { get; set; }
    public string Prompt { get; set; }
    
    public override string ToString()
    {
        return $"{Seconds:F2} сек - {Prompt}";
    }
}