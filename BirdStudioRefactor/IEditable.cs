using System;

namespace BirdStudioRefactor
{
    interface IEditable
    {
        public void performEdit(EditHistoryItem edit);
        public void revertEdit(EditHistoryItem edit);
    }

    class EditTypeNotSupportedException : Exception { }
}
