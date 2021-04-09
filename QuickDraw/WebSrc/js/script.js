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
            console.error("Database error: " + e.target.error);
        };

        var timeInputElems = document.querySelectorAll('#footer > div.time-item > input');
        var timeInterval = localStorage.getItem('timeInterval')
        if (timeInterval === null) {
            timeInterval = '60';
            localStorage.setItem('timeInterval', timeInterval);
        }

        timeInputElems.forEach(timeInputElem => {
            timeInputElem.checked = false;

            if (timeInputElem.value === timeInterval)
            {
                timeInputElem.checked = true;
            }

            timeInputElem.addEventListener('click', e => {
                localStorage.setItem('timeInterval', timeInputElem.value);
            });
        })

        var masterCheckboxElem = document.querySelector('#folder-list-container > div.folder-list-header > div.master-checkbox > input');

        var masterCheckboxEnabled = localStorage.getItem('masterEnabled');
        if (masterCheckboxEnabled === null) {
            localStorage.setItem('masterEnabled', false);
        } else {
            masterCheckboxElem.checked = localStorage.getItem('masterEnabled') === 'true';
        }

        masterCheckboxElem.addEventListener('click', e => {
            var checkboxElems = document.querySelectorAll('#folder-list-container > ul.folder-list > li > div > input');

            var enabledTransaction = db.transaction(['folders'], 'readwrite');
            var objectStore = enabledTransaction.objectStore("folders");

            var masterEnabled = e.target.checked;

            checkboxElems.forEach(checkboxElem => {
                objectStore.get(checkboxElem.parentElement.parentElement.getAttribute('data-folder-path')).onsuccess = e => {
                    var folder = e.target.result;
                    folder.enabled = masterEnabled;
                    objectStore.put(folder);
                };
                checkboxElem.checked = masterEnabled;
            });

            localStorage.setItem('masterEnabled', masterEnabled);
        });

        function UpdateFoldersFromDB() {
            var getAllTransaction = db.transaction(["folders"], "readonly");
    
            var objectStore = getAllTransaction.objectStore("folders");
    
            objectStore.getAll().onsuccess = e => {
                var folders = e.target.result;
    
                var foldersElem = document.querySelector("#folder-list-container > ul.folder-list");
                while (foldersElem.lastChild.nodeType === Node.TEXT_NODE || !foldersElem.lastChild.classList.contains("empty")) {
                    foldersElem.removeChild(foldersElem.lastChild);
                }
    
                for (const folderItem of folders)
                {
                    var clone = template.content.cloneNode(true);

                    var liElem = clone.querySelector('li');
                    liElem.setAttribute("data-folder-path", folderItem.Path)
    
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
                    checkboxElem.checked = folderItem.enabled;
                    checkboxElem.addEventListener('click', e => {
                        if (!e.target.checked)
                        {
                            localStorage.setItem('masterEnabled', e.target.checked);
                            masterCheckboxElem.checked = e.target.checked;
                        }

                        var enabledTransaction = db.transaction(['folders'], 'readwrite');

                        var objectStore = enabledTransaction.objectStore("folders");

                        var folder = folderItem;
                        folder.enabled = e.target.checked;
                        objectStore.put(folder);
                    });

                    foldersElem.appendChild(clone);
                }
            }
        }

        function OpenFolder(path) {
            window.chrome.webview.postMessage(
                {
                    type: "openFolder",
                    path: path
                }
            );
        }

        function RemoveFolder(path) {
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

        UpdateFoldersFromDB();

        var template = document.querySelector("#folder-row");
    
        var addFoldersElem = document.getElementById("add-folders");
        addFoldersElem.addEventListener('click', event => {
            window.chrome.webview.postMessage(
                {
                    type: "addFolders"
                }
            );
        });

        var startElem = document.getElementById("start");
        startElem.addEventListener('click', e => {
            var folderElems = document.querySelectorAll('#folder-list-container > ul.folder-list > li:not(.empty)');
            var folders = [];

            folderElems.forEach(folderElem => {
                var checkboxElem = folderElem.querySelector("input[type='checkbox']");
                
                if (checkboxElem.checked) {
                    folders.push(folderElem.getAttribute('data-folder-path'));
                }
            });

            if (folders.length === 0)
            {
                // no folders selected, do something
            }

            window.chrome.webview.postMessage(
                {
                    type: "getImages",
                    folders: folders,
                    interval: parseInt(localStorage.getItem('timeInterval'))
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
                        folder.enabled = false;
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