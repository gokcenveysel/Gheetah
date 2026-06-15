namespace Gheetah.Models.AiModels
{
    public class ConflictBlock
    {
        public int BlockIndex { get; set; }
        public string FilePath { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string HeadContent { get; set; }
        public string IncomingContent { get; set; }
        public string BaseContent { get; set; }
        public bool IsBddScenario { get; set; }
    }
}
