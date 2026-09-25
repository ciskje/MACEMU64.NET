






#include <stdio.h>
#include <string.h>

void subst(char *filename,char *ori,char *des)
{
  FILE *fh,*fh2;
  char str[160]={ '\0' };
  char tmp[160]={ '\0' };
  char *p;
  int  i,j,k,l;
  
  fh=fopen(filename,"r");
  fh2=fopen("temp","w");
  while(fgets(str,160,fh))
  {
    p=strstr(str,ori);
    if (p!=NULL)
    {
      i=p-str;
      j=i+strlen(ori);
      l=0;
      for(k=0;k<i;k++)
      tmp[l++]=str[k];
      tmp[l++]=0;
      strcat(tmp,des);
      l=strlen(tmp);
      for(k=j;k<strlen(str);k++)
      tmp[l++]=str[k];
      tmp[l++]=0;
    }
    else
    strcpy(tmp,str);
    
    fputs(tmp,fh2);
  }
  fclose(fh2);
  fclose(fh);
  sprintf(str,"copy temp %s >nul",filename);
  system(str);
}

void insnum(char *filename,int ilc)
{
  FILE *fh,*fh2;
  char str[160]={ '\0' };
  char tmp[160]={ '\0' };
  int  i;
  
  fh=fopen(filename,"r");
  fh2=fopen("temp","w");
  i=0;
  while(fgets(str,160,fh))
  {
    if (i==ilc)
    sprintf(tmp,"%d%s",ilc,str);
    else
    sprintf(tmp,"%s",str);
    i++;
    fputs(tmp,fh2);
  }
  fclose(fh2);
  fclose(fh);
  sprintf(str,"copy temp %s >nul",filename);
  system(str);
}

main(int argc,char *argv[])
{
  FILE *fh,*fh2;
  int  ilc;
  char *ori;
  char str[160];
  char tmp[160];
  char des[160];
  
  fh=fopen(argv[1],"r");
  sprintf(str,"copy %s output.pas >nul",argv[1]);
  system(str);
  
  ilc=0;
  while(fgets(str,160,fh))
  {
    if (str[0]=='p')
    {
      strcpy(tmp,strtok(str,":"));
      strcpy(ori,tmp);
      strcat(ori,";");
      
      sprintf(des,"%d;",ilc);
      subst("output.pas",ori,des);
      
      strcpy(ori,tmp);
      strcat(ori,":");
      
      sprintf(des,"%d:",ilc);
      subst("output.pas",ori,des);
    }
    else
    insnum("output.pas",ilc);
    printf("ILC: %d\r",ilc);
    ilc++;
  }
  fclose(fh);
  puts("\n");
}













