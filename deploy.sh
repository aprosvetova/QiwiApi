ApiKey=$1
Source=$2
mono nuget.exe pack QiwiApi.nuspec
nuget push QiwiApi.*.nupkg $ApiKey -Source $Source
