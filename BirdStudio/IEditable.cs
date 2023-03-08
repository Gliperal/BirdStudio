using System;

namespace BirdStudio
{
    public interface IEditable
    {
        public void performEdit(EditHistoryItem edit);
        public void revertEdit(EditHistoryItem edit);
    }

    class EditTypeNotSupportedException : Exception { }
}
