import { useState } from "react";
import { ordersApi } from "./ordersApi.js";
import { ConfirmDialog } from "../../../components/ui/ConfirmDialog.jsx";
export function OrderTransitionForm({ order, changed, stale = false }) {
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState(null);
  const [transition, setTransition] = useState(null);
  return (
    <>
      {notice && (
        <p role={notice.success ? "status" : "alert"}>{notice.text}</p>
      )}
      <section className="card card-body">
        <h2>Order transition</h2>
        <form
          className="add-product-form"
          onSubmit={(e) => {
            e.preventDefault();
            setTransition(Object.fromEntries(new FormData(e.currentTarget)));
          }}
        >
          <label>
            Next order state
            <select className="form-control" name="status">
              {order.availableTransitions.map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </label>
          <label>
            Transition reason
            <textarea
              className="form-control"
              name="reason"
              maxLength="500"
              required
            />
          </label>
          <button className="btn btn-primary" disabled={pending || stale}>
            Review order transition
          </button>
        </form>
      </section>{" "}
      <ConfirmDialog
        open={Boolean(transition)}
        onClose={() => setTransition(null)}
        title="Confirm order transition"
        pending={pending}
        onConfirm={async () => {
          if (pending || stale) return;
          setPending(true);
          try {
            await ordersApi.updateStatus(order.id, transition);
            setNotice({ success: true, text: "Order transition recorded." });
            changed();
          } catch (e) {
            setNotice({
              text:
                e.response?.data?.title ||
                "Order transition could not be saved.",
            });
          } finally {
            setTransition(null);
            setPending(false);
          }
        }}
      >
        {transition?.status}: {transition?.reason}
      </ConfirmDialog>
    </>
  );
}
