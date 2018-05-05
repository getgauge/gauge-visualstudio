#!/bin/bash

repoName=gauge-visualstudio
githubUser=getgauge

go get -v -u github.com/aktau/github-release

version=$(cat version.txt)
release_description=$(ruby -e "$(curl -sSfL https://github.com/getgauge/gauge/raw/master/build/create_release_text.rb)" $repoName $githubUser)

$GOPATH/bin/github-release release -u $githubUser -r $repoName --draft -t "v$version" -d "$release_description" -n "$repoName $version"

for i in `ls *.vsix`; do
    $GOPATH/bin/github-release -v upload -u $githubUser -r $repoName -t "v$version" -n $i -f $i
    if [ $? -ne 0 ];then
        exit 1
    fi
done