(() => {
    var template = document.querySelector("#folder-row");
    console.log(template);

    addFoldersElem = document.getElementById("add-folders");
    addFoldersElem.addEventListener('click', event => {
        window.chrome.webview.postMessage(
            {
                type: "addFolders"
            }
        );
    });

    window.chrome.webview.addEventListener('message', event => {
        var data = event.data;
        
        switch(data.type) {
            case "UpdateFolders":
                foldersElem = document.querySelector("#folder-list-container > ul.folder-list");
                while (foldersElem.lastChild.nodeType === Node.TEXT_NODE || !foldersElem.lastChild.classList.contains("empty")) {
                    foldersElem.removeChild(foldersElem.lastChild);
                }
                
                for (const folderItem of data.data)
                {
                    var clone = template.content.cloneNode(true);
                    var li = clone.querySelector("li");
                    li.setAttribute('data-folder-index', folderItem.Index);
                    var pathElem = clone.querySelector("div.folder-path");
                    pathElem.innerHTML = folderItem.Path;
                    foldersElem.appendChild(clone);
                }
                break;
            default:
                break;
        }
    });
})();