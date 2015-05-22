fileID = fopen('C:\Users\Danny\Desktop\angularVelocityFiltered.csv');
C = textscan(fileID, '%s', 1);

fp = 'E:\CS699-Gleicher\Leap\Leap\LeapUnity\Assets\Matlab';
%fp = ''

if(strcmp('Walking90deg', C{1}))
    fid = fopen(strcat(fp,'\walkingAnnotations.csv'));
elseif(strcmp('WindowWashingA', C{1}))
    fid = fopen(strcat(fp,'\windowWashing.csv'));
elseif(strcmp('PassSodaA', C{1}))
    fid = fopen(strcat(fp,'\PassSodaA.csv'));
elseif(strcmp('PassSodaB', C{1}))
    fid = fopen(strcat(fp,'\PassSodaB.csv'));
end

A = textscan(fid, '%s%s%d%d%s%d%d', 'delimiter', ',');
A{3};

% %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%To compare multiple files, just add another file to this cell array
%structure
B = { 
    textscan( fopen(strcat(fp,'\walking90deg.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1);
    textscan( fopen(strcat(fp,'\windowWashing.csv')), '%s%s%d%d%d%s%d%d%s', 'delimiter', ',', 'HeaderLines', 1); 
    };
  
length(B);
B{1}{3}(1)




