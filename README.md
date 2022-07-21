# SyncCollection
Download an internet archive collection to your pc.

Command line:

  SyncCollection.exe [[rowCount] collection]
 
The default collection, if one is not provided on the command line, is apple_ii_library_4am and 
represents the items shown at the url https://archive.org/details/apple_ii_library_4am and you would
get the same result using the command line:

    SyncCollection.exe apple_ii_library_4am

To download the items represented by the collection shown at https://archive.org/details/softwarelibrary 
you would use the following command line:

  SyncCollection.exe softwarelibrary

To download the first 50 items represented by the collection shown at
https://archive.org/details/softwarelibrary you would use the following command line:

  SyncCollection.exe 50 softwarelibrary
