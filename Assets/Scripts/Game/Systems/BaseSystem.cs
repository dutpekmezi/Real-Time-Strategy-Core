using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSystem : IDisposable
{
    protected BaseSystem()
    {

    }
    protected abstract void Tick();
    public virtual void OnDispose()
    {
        Dispose();
    }
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
