import SharedVideosList from "@/components/SharedVideoList";
import { ReactElement } from "react";


export default function VideoShareList(): ReactElement {
    return (
        <div className="flex flex-col items-center">
            <section className="container mt-10 flex flex-col items-center text-center">
                <SharedVideosList/>
           </section>
        </div>   
    );
}