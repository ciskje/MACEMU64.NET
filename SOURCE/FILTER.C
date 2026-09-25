





#include <stdio.h>
#define INC 2

main(int argc,char *argv[])
{
  int   col=0,c,i,cp;
  int   start=1;
  FILE  *fin,*fout;
  
  puts("FILTER 1.0 - Format c program to a knuth form.");
  puts("Copyright (C) 1995, Ferrara Francesco.\n");
  if (argc!=3)
  {
    puts("filter [filein] [fileout]");
    exit(1);
  }
  fin=fopen(argv[1],"r");
  fout=fopen(argv[2],"w");
  while(!feof(fin))
  {
    loop:
    c=getc(fin);
    if (c=='\r') goto loop;
    if (c=='\t') c=' ';
    
    if (c=='}')
  col-=INC;
  if (col<0) col=0;
  if (start && c!=' ' && c!='\t')
  {
    start=0;
    putc('\n',fout);
    for(i=0;i<col;i++)
    putc(' ',fout);
  }
  if (c=='{')
    col+=INC;
    if (c=='\n')
    start=1;
    if (!start && c!=-1)
    putc(c,fout);
  }
  fclose(fin);
  fclose(fout);
  return(0);
}

