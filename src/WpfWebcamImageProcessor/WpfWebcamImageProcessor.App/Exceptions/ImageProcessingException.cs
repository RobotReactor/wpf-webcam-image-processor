using System;

namespace WpfWebcamImageProcessor.App.Exceptions
{
    /// <summary>
    /// Custom exception class representing errors specifically related to
    /// image processing operations within this application.
    /// Helps distinguish processing failures from other general exceptions.
    /// </summary>
    public class ImageProcessingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProcessingException"/> class.
        /// </summary>
        public ImageProcessingException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProcessingException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ImageProcessingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProcessingException"/> class
        /// with a specified error message and a reference to the inner exception that is
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ImageProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
}