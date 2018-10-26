namespace MFG124959.Classes.InventorAddIn
{
    [System.ComponentModel.DesignerCategory("")]
    internal class PictureConverter : System.Windows.Forms.AxHost
    {

        private PictureConverter() : base(string.Empty)
        {
        }

        public static stdole.IPictureDisp ImageToPictureDisp(System.Drawing.Image image)
        {
            return (stdole.IPictureDisp)GetIPictureDispFromPicture(image);
        }
    }
}