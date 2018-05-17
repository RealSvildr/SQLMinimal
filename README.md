# SQLMinimal v 1.2

This library was made based on System.Data.Entity, but with the objective to be smaller and faster.

Complied Lib: https://github.com/RealSvildr/SQLMinimal/blob/master/SQLMinimal.dll

Change Notes:

* v1.2
  * Included FileStream function to use with video streaming or data streaming
* v1.0
  * Auto generate the string connection
  * Function ExecuteSqlCommand for functions with no return
  * Function SQLQuery for functions with return
  
It uses Entity Framework 2.5

P.S.: If you don't want to use those Extensions methods, you can remove the file, but you have to paste the class Object into the DBContext file and change it to private.

TODO: 
- [ ] Create it's own method of Commit, Save and Rollback
  
