declare class EasyMDE {
    public value(newValue: string): void;
    public value(): string;

    public constructor(options: { toolbar: boolean, spellChecker: boolean, status: boolean, indentWithTabs: boolean, autoDownloadFontAwesome: boolean, forceSync: boolean });
}

var easyMDE: EasyMDE;

document.addEventListener("DOMContentLoaded", () => {
    easyMDE = new EasyMDE({ toolbar: false, spellChecker: false, status: false, indentWithTabs: false, autoDownloadFontAwesome: false, forceSync: true });

    let searchBookButton = document.getElementById("search-book") as HTMLInputElement;
    let isbnInput = document.getElementById("isbn") as HTMLInputElement;

    isbnInput.addEventListener("input", () => {
        isbnInput.value = isbnInput.value.replace("-", "").replace("\t", "").trim();
        searchBookButton.disabled = !isbnInput.checkValidity();
        isbnInput.reportValidity();
    });

    searchBookButton.addEventListener("click", () => SearchBookInfo(isbnInput.value));

    document.getElementById("coverUrl").addEventListener("input", () => SetCoverImage((document.getElementById("coverUrl") as HTMLInputElement).value));
});

function SetCoverImage(coverUrl: string) {
    if (/\/img\/[0-9]{13}\.[a-z]{3,4}/.test(coverUrl)) {
        coverUrl = "https://readinglist.laedit.net" + coverUrl;
    }
    document.getElementById("cover-img").setAttribute("src", coverUrl);
}

async function SearchBookInfo(isbn: string) {
    let formElements = Array.from(document.getElementById("submit-book-form").children);
    formElements.forEach(element => (element as HTMLInputElement).disabled = true);

    // loader
    let searchBookButton = document.getElementById("search-book");
    searchBookButton.firstChild.remove();
    var dotFlashingDiv = document.createElement("div");
    dotFlashingDiv.classList.add("dot-flashing");
    searchBookButton.appendChild(dotFlashingDiv);

    try {
        let rawResponse = await fetch(`/bookinfo/${isbn}`, { credentials: "same-origin" });
        let response = await rawResponse.json();
        if (response.status === "ok") {
            console.info(response);
            (document.getElementById("title") as HTMLInputElement).value = response.book.title;
            (document.getElementById("author") as HTMLInputElement).value = response.book.author;
            (document.getElementById("editor") as HTMLInputElement).value = response.book.editor;
            (document.getElementById("coverUrl") as HTMLInputElement).value = response.book.coverUrl;
            SetCoverImage(response.book.coverUrl);
            if (response.book.summary) {
                easyMDE.value(response.book.summary);
            }
        }
        else if (response.status === "error") {
            console.error(response);
            DisplayInfoMessage("Book not found", "warning");
        }
        else {
            console.error(response);
            DisplayInfoMessage(response.message + "\r\n\r\n" + response.details, "error");
        }
    }
    catch (error) {
        console.error(error);
        DisplayInfoMessage(error, "error");
    }

    searchBookButton.firstChild.remove();
    searchBookButton.appendChild(document.createTextNode("Search"));
    formElements.forEach(element => (element as HTMLInputElement).disabled = false);
}

function DisplayInfoMessage(message: string, type: "error" | "warning" | "success") {
    var infoDiv = document.createElement("div");
    infoDiv.classList.add("info");
    infoDiv.classList.add(type);
    infoDiv.append(message);
    document.getElementsByTagName("main")[0].insertBefore(infoDiv, document.getElementById("submit-book-form"));
}
