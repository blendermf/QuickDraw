(() => {
    var request = window.indexedDB.open("QuickDraw");

    request.onerror = e => {
        console.error("Database open error: " + e.target.error);
    };

    request.onupgradeneeded = e => {
        var db = e.target.result;

        var objectStore = db.createObjectStore("folders", { keyPath: "Path"});
    };

    request.onsuccess = e => {
        var db = e.target.result;

        db.onerror = e => {
            console.log(e);
            console.error("Database error: " + e.target.error);
        };

        var masterCheckboxElem = document.querySelector('#folder-list-container > div.folder-list-header > div.master-checkbox > input');

        function UpdateFoldersFromDB() {
            var getAllTransaction = db.transaction(["folders"], "readonly");
    
            var objectStore = getAllTransaction.objectStore("folders");
    
            objectStore.getAll().onsuccess = e => {
                var folders = e.target.result;
    
                foldersElem = document.querySelector("#folder-list-container > ul.folder-list");
                while (foldersElem.lastChild.nodeType === Node.TEXT_NODE || !foldersElem.lastChild.classList.contains("empty")) {
                    foldersElem.removeChild(foldersElem.lastChild);
                }
    
                for (const folderItem of folders)
                {
                    var clone = template.content.cloneNode(true);
    
                    var pathElem = clone.querySelector("div.folder-path");
                    pathElem.innerHTML = folderItem.Path;
    
                    var countElem = clone.querySelector("div.folder-image-count");
                    countElem.innerHTML = folderItem.Count;

                    var openFolderElem = clone.querySelector("button.open-folder");
                    openFolderElem.addEventListener("click", e => {
                        OpenFolder(folderItem.Path);
                    });

                    var removeFolderElem = clone.querySelector("button.remove-folder");
                    removeFolderElem.addEventListener("click", e => {
                        RemoveFolder(folderItem.Path);
                    });

                    var checkboxElem = clone.querySelector("input[type='checkbox']");
                    checkboxElem.addEventListener('click', e => {
                        if (!e.target.checked)
                        {
                            masterCheckboxElem.checked = e.target.checked;
                        }
                    });

                    foldersElem.appendChild(clone);
                }
            }
        }

        function OpenFolder(path) {
            console.log("Open Folder: " + path);

            window.chrome.webview.postMessage(
                {
                    type: "openFolder",
                    path: path
                }
            );
        }

        function RemoveFolder(path) {
            console.log("Remove Folder: " + path);

            var removeTransaction = db.transaction(["folders"], "readwrite");

            var objectStore = removeTransaction.objectStore("folders");
                objectStore.count(path).onsuccess = e => {
                    if (e.target.result > 0)
                    {
                        objectStore.delete(path);
                    }
                };

            removeTransaction.onerror = e => {
                console.error("Transaction Error: " + e.target.error);
            }

            removeTransaction.oncomplete = e => {
                UpdateFoldersFromDB();
            };
        }
        
        masterCheckboxElem.addEventListener('click', e => {
            var checkboxElems = document.querySelectorAll('#folder-list-container > ul.folder-list > li > div > input');

            checkboxElems.forEach(checkboxElem => {
                console.log(checkboxElem);
                checkboxElem.checked = e.target.checked;
            });
        });

        UpdateFoldersFromDB();

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
                case "AddFolders":
                    var addTransaction = db.transaction(["folders"], "readwrite");

                    var objectStore = addTransaction.objectStore("folders");
                    data.data.forEach(folder => {
                        objectStore.count(folder.Path).onsuccess = e => {
                            if (e.target.result > 0)
                            {
                                objectStore.put(folder);
                            } else {
                                objectStore.add(folder);
                            }
                        };
                    });

                    addTransaction.onerror = e => {
                        console.error("Transaction Error: " + e.target.error);
                    }

                    addTransaction.oncomplete = e => {
                        UpdateFoldersFromDB();
                    };
                    break;
                default:
                    break;
            }
        });
    };
})();