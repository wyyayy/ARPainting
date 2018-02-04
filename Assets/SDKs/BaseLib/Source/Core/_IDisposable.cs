/**
 * Defines a method to release allocated resources. 
 * @author Spark
 */
public interface _IDisposable
{
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources. 
    void Dispose();

    /// Check if an object is disposed.
    bool IsDisposed();
}