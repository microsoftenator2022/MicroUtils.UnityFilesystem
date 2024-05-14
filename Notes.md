## AssetBundles
An archive of sorts that can contain multiple "files". These may be structured (serialized file)
or unstructured (effectively just a big byte array)

UnityDataTools "mounts" these archives in a kind of "file system" so these files have a "path"
relative to the root "mount point" (is the mount point for bundles always `archive://`?)

I do not consider some low-level details because UnityDataTools library should handle these (see: `UnityFile`)
eg. Compression, endianness conversions, etc.

## `SerializedFile`
These (may) contain:
- Type trees - `TypeTreeNode`
- Objects - `ObjectInfo`: object type, path ID, data offset
- External references - `ExternalReference`: path to some other file

## `TypeTreeNode`
Serialized files describe structured data using type trees (`TypeTreeNode`)

The described data is something like a variable-sized struct: Field values may be "inlined" ie. serialized in-place.

Type trees are stored in the bundle alongside the data (with some exceptions that I do not currently handle)

A type tree corresponds roughly to a C# class.

There are some "primitive" types: C# integral types (integer and floating-point numbers, characters (UTF-8 encoding?)),
arrays (of any type), strings (special case arrays)

The tree is parsed in-order and depth-first. Each node's offset is from the end of the previous value.
There may be padding added to the end of a value for "alignment" or to its child nodes (indicated by flags on the type 
tree node). **Therefore: you must parse every node**.

## "Special" object types
- `StreamingInfo` and `StreamedResource`: Pointer to unstructured data (ie. `byte[]`)
- `PPtr`: Typed reference to some other object by path ID (`ObjectInfo.Id`) and file ID.
    
    If `fileId == 0`, the reference points to an object the same serialized file.
    
    If `fileId > 0` then `fileId - 1` is an index into the external references array.
    
- Fixed-size array: a set values named `data[i]`, where `i` is the index.
